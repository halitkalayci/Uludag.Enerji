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

        public async Task RemoveAsync(IEnumerable<string> keys)
        {
            RedisKey[] redisKeys = keys.Select(BuildKey).ToArray();
            if (redisKeys.Length == 0)
                return;

            // Tek komut: DEL key1 key2 ...
            await Database.KeyDeleteAsync(redisKeys);
        }

        // SADD + süre ayarı tek Lua script'inde -> atomik.
        // Süre yalnızca uzatılır: PTTL < istenen ise PEXPIRE. (Yeni oluşan kümenin süresi yoktur, PTTL = -1 -> ayarlanır.)
        // "PEXPIRE ... GT" kullanılmaz: süresiz anahtarı sonsuz kabul ettiği için yeni kümeye süre koymaz.
        private const string AddToSetScript =
            "redis.call('SADD', KEYS[1], ARGV[1]) " +
            "local ttl = redis.call('PTTL', KEYS[1]) " +
            "if ttl < tonumber(ARGV[2]) then redis.call('PEXPIRE', KEYS[1], ARGV[2]) end " +
            "return 1";

        public async Task AddToSetAsync(string key, string member, TimeSpan expiration)
        {
            await Database.ScriptEvaluateAsync(
                AddToSetScript,
                [BuildKey(key)],
                [member, (long)expiration.TotalMilliseconds]);
        }

        // SMEMBERS + DEL tek Lua script'inde -> atomik.
        private const string PopAllFromSetScript =
            "local members = redis.call('SMEMBERS', KEYS[1]) " +
            "redis.call('DEL', KEYS[1]) " +
            "return members";

        public async Task<IReadOnlyCollection<string>> PopAllFromSetAsync(string key)
        {
            RedisResult result = await Database.ScriptEvaluateAsync(PopAllFromSetScript, [BuildKey(key)]);
            RedisValue[] members = (RedisValue[]?)result ?? [];
            return members.Select(member => member.ToString()).ToList();
        }

        private RedisKey BuildKey(string key) => instanceName + key;
    }
}
