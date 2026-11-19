using StackExchange.Redis;
using System.Text.Json;

namespace Uludag.Core.Caching.Redis
{
    // ICacheService'in Redis ile yazılmış hali.
    // Değerler Redis'e JSON string olarak yazılır (STRING tipi + TTL); Redis Insight'ta okunabilir görünür.
    public class RedisCacheService : ICacheService
    {
        private readonly IConnectionMultiplexer connectionMultiplexer;
        private readonly string instanceName;
        public RedisCacheService(IConnectionMultiplexer connectionMultiplexer, RedisOptions redisOptions)
        {
            this.connectionMultiplexer = connectionMultiplexer;
            instanceName = redisOptions.InstanceName;
        }

        // GetDatabase() hafiftir; her çağrıda alınabilir. Asıl pahalı olan bağlantı (multiplexer) tek bir tanedir.
        private IDatabase Database => connectionMultiplexer.GetDatabase();

        public async Task<T?> GetAsync<T>(string key)
        {
            RedisValue value = await Database.StringGetAsync(BuildKey(key));
            return value.IsNullOrEmpty ? default : JsonSerializer.Deserialize<T>(value.ToString());
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan expiration)
        {
            await Database.StringSetAsync(BuildKey(key), JsonSerializer.Serialize(value), expiration);
        }

        public async Task<bool> SetIfExistsAsync<T>(string key, T value)
        {
            // Tek komut, atomik: SET key value XX KEEPTTL
            //   XX      -> yalnızca anahtar varsa yaz (When.Exists)
            //   KEEPTTL -> mevcut süreyi koru
            return await Database.StringSetAsync(BuildKey(key), JsonSerializer.Serialize(value), expiry: null, keepTtl: true, when: When.Exists);
        }

        // INCR ve ilk artıştaki PEXPIRE tek Lua script'inde çalışır -> Redis'te atomik.
        // (İki ayrı komut olsaydı, arada bağlantı koparsa sayaç süresiz kalabilirdi.)
        private const string IncrementScript =
            "local value = redis.call('INCR', KEYS[1]) " +
            "if value == 1 then redis.call('PEXPIRE', KEYS[1], ARGV[1]) end " +
            "return value";

        public async Task<long> IncrementAsync(string key, TimeSpan expiration)
        {
            RedisResult result = await Database.ScriptEvaluateAsync(
                IncrementScript,
                [BuildKey(key)],
                [(long)expiration.TotalMilliseconds]);
            return (long)result;
        }

        public async Task RemoveAsync(string key)
        {
            await Database.KeyDeleteAsync(BuildKey(key));
        }

        private RedisKey BuildKey(string key) => instanceName + key;
    }
}
