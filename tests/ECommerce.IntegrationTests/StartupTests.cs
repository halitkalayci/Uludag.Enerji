using System.Text.Json;
using ECommerce.Domain.Entities;
using ECommerce.Domain.ValueObjects;
using ECommerce.IntegrationTests.Infrastructure;
using ECommerce.Persistence.Contexts;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Uludag.Core.Caching;
using Uludag.Core.Caching.Redis;

namespace ECommerce.IntegrationTests
{
    // Uygulamanın gerçek altyapıyla (yeni açılmış SQL Server + Redis container'ları) ayağa kalktığını kanıtlar (spec 0001, AC-9).
    [Collection(IntegrationCollection.Name)]
    [Trait("Category", "Integration")]
    public class StartupTests(ApiFixture fixture)
    {
        // Yalnızca bu testin yazdığı ürün: temiz bir veritabanında asla bulunmaz.
        private const string FreshRunMarkerProductName = "integration-test-fresh-run-marker";

        [Fact]
        public async Task Startup_FreshContainers_UsesContainerDatabase()
        {
            await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
            UludagDbContext dbContext = scope.ServiceProvider.GetRequiredService<UludagDbContext>();

            string expectedDataSource = new SqlConnectionStringBuilder(fixture.SqlConnectionString).DataSource;

            // Ayar override'ı tutmasaydı uygulama appsettings.json'daki yerel sunucuya bağlanırdı (plan 0001, R-4).
            Assert.Equal(expectedDataSource, dbContext.Database.GetDbConnection().DataSource);
        }

        [Fact]
        public async Task Startup_FreshContainers_HasNoPendingMigrations()
        {
            await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
            UludagDbContext dbContext = scope.ServiceProvider.GetRequiredService<UludagDbContext>();
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;

            IEnumerable<string> appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync(cancellationToken);
            IEnumerable<string> pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);

            Assert.NotEmpty(appliedMigrations);
            Assert.Empty(pendingMigrations);
        }

        [Fact]
        public async Task CacheService_SetValue_IsReadableFromRedisContainer()
        {
            ICacheService cacheService = fixture.Factory.Services.GetRequiredService<ICacheService>();
            RedisOptions redisOptions = fixture.Factory.Services.GetRequiredService<RedisOptions>();
            string key = $"integration-test:{Guid.NewGuid():N}";
            const string value = "Uludağ";

            await cacheService.SetAsync(key, value, TimeSpan.FromMinutes(1));

            // Uygulamanın cache servisi üzerinden değil, doğrudan container'daki Redis'ten okunur:
            // değer in-memory yedekte kalmış olsaydı burada bulunamazdı.
            await using ConnectionMultiplexer redis = await ConnectionMultiplexer.ConnectAsync(fixture.RedisConnectionString);
            RedisValue storedValue = await redis.GetDatabase().StringGetAsync(redisOptions.InstanceName + key);

            Assert.True(storedValue.HasValue, "Değer Redis container'ında bulunamadı.");
            Assert.Equal(value, JsonSerializer.Deserialize<string>(storedValue.ToString()));
        }

        [Fact]
        public async Task Database_FreshRun_StartsWithoutMarkerRow()
        {
            await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
            UludagDbContext dbContext = scope.ServiceProvider.GetRequiredService<UludagDbContext>();
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;

            // Önceki bir koşunun verisi bu koşuya geçseydi işaret ürünü burada bulunurdu.
            Assert.False(await dbContext.Products.AnyAsync(p => p.Name == FreshRunMarkerProductName, cancellationToken),
                "Veritabanı temiz başlamadı: önceki koşunun işaret ürünü bulundu.");

            dbContext.Products.Add(new Product
            {
                Id = Guid.NewGuid(),
                Name = FreshRunMarkerProductName,
                Price = new Money(1m, "TRY"),
                Stock = 1
            });
            await dbContext.SaveChangesAsync(cancellationToken);

            Assert.True(await dbContext.Products.AnyAsync(p => p.Name == FreshRunMarkerProductName, cancellationToken));
        }
    }
}
