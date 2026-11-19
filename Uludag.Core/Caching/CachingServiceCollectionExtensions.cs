using StackExchange.Redis;
using Uludag.Core.Caching;
using Uludag.Core.Caching.InMemory;
using Uludag.Core.Caching.Redis;

// Kullanan projeler ekstra using yazmadan services.AddCacheService(...) diyebilsin diye
// uzantı, DI'ın kendi namespace'inde tanımlanır.
namespace Microsoft.Extensions.DependencyInjection
{
    public static class CachingServiceCollectionExtensions
    {
        // ICacheService kaydı. Hangi implementasyonun kullanılacağı ayara göre seçilir:
        //   - RedisOptions.ConnectionString dolu -> RedisCacheService (docker-compose'daki Redis).
        //   - Boş / bölüm yok                   -> InMemoryCacheService (Redis olmadan, uygulama belleğinde).
        public static IServiceCollection AddCacheService(this IServiceCollection services, RedisOptions? redisOptions)
        {
            ArgumentNullException.ThrowIfNull(services);

            if (string.IsNullOrWhiteSpace(redisOptions?.ConnectionString))
            {
                services.AddMemoryCache();
                services.AddSingleton<ICacheService, InMemoryCacheService>();
                return services;
            }

            // Bağlantı metni hatalıysa uygulama ilk istekte değil, açılışta hata verir.
            ConfigurationOptions configurationOptions = ConfigurationOptions.Parse(redisOptions.ConnectionString);
            // false -> Redis o an ayakta değilse uygulama çökmez; bağlantı arka planda tekrar denenir.
            configurationOptions.AbortOnConnectFail = false;

            services.AddSingleton(redisOptions);
            // Singleton: ConnectionMultiplexer pahalıdır ve thread-safe'dir; uygulama boyunca TEK bağlantı paylaşılır.
            // İlk ihtiyaç duyulduğunda (ilk login / ilk istek) bağlanır.
            services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(configurationOptions));
            services.AddSingleton<ICacheService, RedisCacheService>();

            return services;
        }
    }
}
