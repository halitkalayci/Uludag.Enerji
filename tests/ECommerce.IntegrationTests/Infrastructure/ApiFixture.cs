using System.Security.Cryptography;
using ECommerce.Persistence.Contexts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using Testcontainers.Redis;

namespace ECommerce.IntegrationTests.Infrastructure
{
    // Entegrasyon koşusu başına bir kez: yeni SQL Server + Redis container'ları açılır, uygulama süreç içinde
    // bunlara karşı ayağa kalkar ve şema migration'larla kurulur. Koşu bitince (başarısızlıkta da) uygulama ve
    // container'lar aynı adım içinde kapatılır — hiçbir veri sonraki koşuya geçmez (docs/testing.md, Resource hygiene 5).
    public sealed class ApiFixture : IAsyncLifetime
    {
        // docker-compose.yml ile aynı imajlar.
        private const string SqlServerImage = "mcr.microsoft.com/mssql/server:2022-latest";
        private const string RedisImage = "redis:8-alpine";

        private readonly MsSqlContainer sqlContainer = new MsSqlBuilder(SqlServerImage).Build();
        private readonly RedisContainer redisContainer = new RedisBuilder(RedisImage).Build();

        // MFA anahtar URI'sinde görünmesi beklenen uygulama adı (spec 0002, AC-7): testin kendi verdiği değer.
        public const string MultiFactorIssuer = "Uludag Integration Tests";

        // Test sunucusunda istemci IP'si yoktur; tüm istekler tek bir rate limit kovasını paylaşır.
        // Rate limiting Auth testlerinin konusu değil (spec 0002, Out of scope) — kova pratikte hiç dolmaz.
        private const int RateLimitCapacity = 1_000_000;

        // Fake ödeme paylaşılan host'ta hep onaylar (docs/testing.md, Determinism; spec 0008 / A-1): aksi halde sipariş
        // veren her test %25 olasılıkla kırmızı olur. Ret isteyen testler türetilmiş host ya da stub kullanır (OrderArrange).
        private const string FakePaymentDeclineRate = "0";

        public WebApplicationFactory<Program> Factory { get; private set; } = null!;

        // Token imza anahtarı: testler süresi dolmuş/sahte token üretebilsin diye dışa açıktır (spec 0003).
        public string TokenSecurityKey { get; } = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        public string SqlConnectionString => sqlContainer.GetConnectionString();

        public string RedisConnectionString => redisContainer.GetConnectionString();

        public async ValueTask InitializeAsync()
        {
            await Task.WhenAll(sqlContainer.StartAsync(), redisContainer.StartAsync());

            // "Testing" ortamında appsettings.Development.json ve user-secrets yüklenmez: repodaki secret'lar okunmaz,
            // AdminSeed tanımsız olduğundan açılışta seed çalışmaz. Gereken ayarlar burada, testin içinde verilir.
            Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.UseSetting("ConnectionStrings:ECommerce", SqlConnectionString);
                builder.UseSetting("RedisOptions:ConnectionString", RedisConnectionString);
                builder.UseSetting("TokenOptions:SecurityKey", TokenSecurityKey);
                builder.UseSetting("MultiFactorOptions:Issuer", MultiFactorIssuer);
                builder.UseSetting("RateLimitOptions:Capacity", RateLimitCapacity.ToString(System.Globalization.CultureInfo.InvariantCulture));
                builder.UseSetting("FakePayment:DeclineRate", FakePaymentDeclineRate);
            });

            await using AsyncServiceScope scope = Factory.Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<UludagDbContext>().Database.MigrateAsync();
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (Factory is not null)
                    await Factory.DisposeAsync();
            }
            finally
            {
                await Task.WhenAll(sqlContainer.DisposeAsync().AsTask(), redisContainer.DisposeAsync().AsTask());
            }
        }
    }

    // Tüm entegrasyon test sınıfları aynı container çiftini paylaşır.
    [CollectionDefinition(Name)]
    public sealed class IntegrationCollection : ICollectionFixture<ApiFixture>
    {
        public const string Name = "Integration";
    }
}
