using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Uludag.Core.Caching.Redis;

namespace Uludag.Core.RateLimiting.Redis
{
    // ILeakyBucketRateLimiter'ın Redis ile yazılmış hali. Kovalar tüm uygulama instance'ları arasında paylaşılır.
    // Her kova bir HASH'tir: "uludag:rate-limit:<anahtar>" -> { level, lastLeak }.
    public class RedisLeakyBucketRateLimiter : ILeakyBucketRateLimiter
    {
        // Sızdır + kontrol et + ekle tek Lua script'inde çalışır -> Redis'te atomik.
        // (Ayrı komutlar olsaydı aynı anda gelen iki istek aynı seviyeyi okuyup kovayı kapasitenin üstüne taşırdı.)
        // Zaman Redis'in TIME komutundan alınır: birden fazla instance'ın saatleri farklı olsa da kova tek saate göre sızar.
        //   KEYS[1] -> kova anahtarı, ARGV[1] -> kapasite, ARGV[2] -> sızma aralığı (ms)
        //   Dönüş   -> { 1, 0 } kabul, { 0, bir sonraki damlaya kalan ms } red.
        // Anahtarın süresi Capacity * aralık: dolu kova en geç bu sürede boşalır, boş kova = anahtar yok.
        private const string AcquireScript = """
            local time = redis.call('TIME')
            local now = tonumber(time[1]) * 1000 + math.floor(tonumber(time[2]) / 1000)
            local capacity = tonumber(ARGV[1])
            local interval = tonumber(ARGV[2])

            local data = redis.call('HMGET', KEYS[1], 'level', 'lastLeak')
            local level = tonumber(data[1]) or 0
            local lastLeak = tonumber(data[2]) or now

            local drips = math.floor((now - lastLeak) / interval)
            if drips > 0 then
                level = math.max(0, level - drips)
                lastLeak = lastLeak + drips * interval
            end
            if level == 0 then
                lastLeak = now
            end

            if level + 1 > capacity then
                return { 0, lastLeak + interval - now }
            end

            redis.call('HSET', KEYS[1], 'level', level + 1, 'lastLeak', lastLeak)
            redis.call('PEXPIRE', KEYS[1], capacity * interval)
            return { 1, 0 }
            """;

        // Rate limit kontrolüne tanınan en uzun süre. Sağlıklı bir Redis bu script'i milisaniyenin altında çalıştırır.
        private static readonly TimeSpan RedisCallTimeout = TimeSpan.FromMilliseconds(200);

        private readonly IConnectionMultiplexer connectionMultiplexer;
        private readonly string keyPrefix;
        private readonly int capacity;
        private readonly long leakIntervalMilliseconds;
        private readonly ILogger<RedisLeakyBucketRateLimiter> logger;
        public RedisLeakyBucketRateLimiter(
            IConnectionMultiplexer connectionMultiplexer,
            RedisOptions redisOptions,
            LeakyBucketOptions leakyBucketOptions,
            ILogger<RedisLeakyBucketRateLimiter> logger)
        {
            this.connectionMultiplexer = connectionMultiplexer;
            keyPrefix = redisOptions.InstanceName + "rate-limit:";
            capacity = leakyBucketOptions.Capacity;
            leakIntervalMilliseconds = leakyBucketOptions.LeakIntervalSeconds * 1000L;
            this.logger = logger;
        }

        public async Task<RateLimitResult> TryAcquireAsync(string key)
        {
            // Bağlantı yokken komut gönderilirse StackExchange.Redis onu kuyrukta bekletir ve her istek zaman aşımına
            // kadar (saniyeler) asılı kalır. Bağlantı yoksa Redis'e hiç gidilmez, istek hemen geçirilir (fail-open).
            if (!connectionMultiplexer.IsConnected)
            {
                logger.LogWarning("Rate limit kontrolü yapılamadı (Redis bağlantısı yok); istek limitsiz geçirildi. Anahtar: {Key}", key);
                return RateLimitResult.Allowed;
            }

            try
            {
                // Redis bağlantıyı kapatmadan donarsa (ağ kopması, pause, uzun AOF beklemesi) IsConnected true kalır ve
                // komut varsayılan 5 sn zaman aşımını bekler. Middleware her isteğin önünde çalıştığı için bu süre
                // tüm API'ye eklenirdi -> çağrı kısa bir süreyle sınırlanır, aşılırsa istek geçirilir.
                RedisResult result = await connectionMultiplexer.GetDatabase().ScriptEvaluateAsync(
                    AcquireScript,
                    [keyPrefix + key],
                    [capacity, leakIntervalMilliseconds])
                    .WaitAsync(RedisCallTimeout);

                RedisResult[] values = (RedisResult[])result!;
                return (long)values[0] == 1
                    ? RateLimitResult.Allowed
                    : RateLimitResult.Rejected(TimeSpan.FromMilliseconds((long)values[1]));
            }
            // Redis'e ulaşılamıyor / zaman aşımı -> istek geçirilir (fail-open).
            // Cache altyapısının çökmesi tüm API'yi kilitlemesin; bu sürede limit uygulanmaz.
            // Dikkat: RedisTimeoutException, RedisException'dan DEĞİL TimeoutException'dan türer; WaitAsync de
            // TimeoutException fırlatır -> ikisi ayrıca yakalanır.
            catch (Exception exception) when (exception is RedisException or TimeoutException)
            {
                logger.LogWarning(exception, "Rate limit kontrolü yapılamadı (Redis erişilemiyor); istek limitsiz geçirildi. Anahtar: {Key}", key);
                return RateLimitResult.Allowed;
            }
        }
    }
}
