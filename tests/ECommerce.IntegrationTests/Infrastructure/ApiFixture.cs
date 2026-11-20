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

        public WebApplicationFactory<Program> Factory { get; private set; } = null!;

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
                builder.UseSetting("TokenOptions:SecurityKey", Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)));
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
