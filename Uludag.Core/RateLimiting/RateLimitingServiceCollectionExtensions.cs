using Uludag.Core.Caching.Redis;
using Uludag.Core.RateLimiting;
using Uludag.Core.RateLimiting.InMemory;
using Uludag.Core.RateLimiting.Redis;

// Kullanan projeler ekstra using yazmadan services.AddLeakyBucketRateLimiter(...) diyebilsin diye
// uzantı, DI'ın kendi namespace'inde tanımlanır.
namespace Microsoft.Extensions.DependencyInjection
{
    public static class RateLimitingServiceCollectionExtensions
    {
        // ILeakyBucketRateLimiter kaydı. Hangi implementasyonun kullanılacağı AddCacheService ile aynı kurala göre seçilir:
        //   - RedisOptions.ConnectionString dolu -> RedisLeakyBucketRateLimiter (kovalar Redis'te, instance'lar arası paylaşılır).
        //   - Boş / bölüm yok                   -> InMemoryLeakyBucketRateLimiter (kovalar uygulama belleğinde).
        // Redis modunda bağlantı (IConnectionMultiplexer) AddCacheService'ten gelir -> önce services.AddCacheService(...) çağrılmalıdır.
        // Ayarlar eksik / hatalıysa uygulama ilk istekte değil, açılışta hata verir.
        public static IServiceCollection AddLeakyBucketRateLimiter(this IServiceCollection services, LeakyBucketOptions leakyBucketOptions, RedisOptions? redisOptions)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(leakyBucketOptions);

            if (leakyBucketOptions.Capacity <= 0)
                throw new ArgumentException("RateLimitOptions.Capacity sıfırdan büyük olmalıdır.", nameof(leakyBucketOptions));
            if (leakyBucketOptions.LeakIntervalSeconds <= 0)
                throw new ArgumentException("RateLimitOptions.LeakIntervalSeconds sıfırdan büyük olmalıdır.", nameof(leakyBucketOptions));

            // Singleton: ayarlar uygulama boyunca değişmez; bellek implementasyonu kovaları kendi içinde tutar.
            services.AddSingleton(leakyBucketOptions);

            if (string.IsNullOrWhiteSpace(redisOptions?.ConnectionString))
            {
                services.AddSingleton(TimeProvider.System);
                services.AddSingleton<ILeakyBucketRateLimiter, InMemoryLeakyBucketRateLimiter>();
                return services;
            }

            services.AddSingleton<ILeakyBucketRateLimiter, RedisLeakyBucketRateLimiter>();
            return services;
        }
    }
}
