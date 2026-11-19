using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace Uludag.Core.Caching.InMemory
{
    // ICacheService'in uygulama belleğinde çalışan hali. Redis yokken (bağlantı ayarı boş) kullanılır.
    // Redis ile aynı davranması için değerler JSON olarak saklanır: okuyan taraf nesnenin kopyasını alır,
    // sakladığı nesneyi sonradan değiştirse bile cache'teki değer bozulmaz.
    // Dikkat: veri uygulama kapanınca kaybolur ve birden fazla instance arasında paylaşılmaz.
    public class InMemoryCacheService : ICacheService
    {
        // Bitiş anı değerle birlikte tutulur: SetIfExistsAsync, Redis'teki KEEPTTL gibi süreyi koruyabilsin diye.
        private sealed record CacheEntry(string Json, DateTimeOffset ExpiresAt);

        private readonly IMemoryCache memoryCache;
        public InMemoryCacheService(IMemoryCache memoryCache)
        {
            this.memoryCache = memoryCache;
        }

        public Task<T?> GetAsync<T>(string key)
        {
            T? value = memoryCache.TryGetValue(key, out CacheEntry? entry) && entry is not null
                ? JsonSerializer.Deserialize<T>(entry.Json)
                : default;
            return Task.FromResult(value);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan expiration)
        {
            DateTimeOffset expiresAt = DateTimeOffset.UtcNow.Add(expiration);
            memoryCache.Set(key, new CacheEntry(JsonSerializer.Serialize(value), expiresAt), expiresAt);
            return Task.CompletedTask;
        }

        public Task<bool> SetIfExistsAsync<T>(string key, T value)
        {
            if (!memoryCache.TryGetValue(key, out CacheEntry? entry) || entry is null)
                return Task.FromResult(false);

            // Aynı bitiş anıyla yeniden yazılır -> kalan süre değişmez.
            memoryCache.Set(key, entry with { Json = JsonSerializer.Serialize(value) }, entry.ExpiresAt);
            return Task.FromResult(true);
        }

        public Task<long> IncrementAsync(string key, TimeSpan expiration)
        {
            // IMemoryCache'te atomik "oku-artır-yaz" yok -> kilit ile tek seferde bir istek.
            lock (incrementLock)
            {
                bool exists = memoryCache.TryGetValue(key, out CacheEntry? entry) && entry is not null;
                long value = exists ? JsonSerializer.Deserialize<long>(entry!.Json) + 1 : 1;
                // Sabit pencere: süre ilk artışta başlar, sonraki artışlar bitiş anını değiştirmez (Redis tarafıyla aynı).
                DateTimeOffset expiresAt = exists ? entry!.ExpiresAt : DateTimeOffset.UtcNow.Add(expiration);

                memoryCache.Set(key, new CacheEntry(JsonSerializer.Serialize(value), expiresAt), expiresAt);
                return Task.FromResult(value);
            }
        }

        private readonly object incrementLock = new();

        public Task RemoveAsync(string key)
        {
            memoryCache.Remove(key);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(IEnumerable<string> keys)
        {
            foreach (string key in keys)
                memoryCache.Remove(key);
            return Task.CompletedTask;
        }

        public Task AddToSetAsync(string key, string member, TimeSpan expiration)
        {
            // Oku-ekle-yaz atomik olmalı -> PopAllFromSetAsync ile aynı kilit.
            lock (setLock)
            {
                bool exists = memoryCache.TryGetValue(key, out CacheEntry? entry) && entry is not null;
                HashSet<string> members = exists ? JsonSerializer.Deserialize<HashSet<string>>(entry!.Json) ?? [] : [];
                members.Add(member);

                // Süre yalnızca uzatılır (Redis tarafıyla aynı).
                DateTimeOffset requestedExpiresAt = DateTimeOffset.UtcNow.Add(expiration);
                DateTimeOffset expiresAt = exists && entry!.ExpiresAt > requestedExpiresAt ? entry.ExpiresAt : requestedExpiresAt;

                memoryCache.Set(key, new CacheEntry(JsonSerializer.Serialize(members), expiresAt), expiresAt);
            }
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<string>> PopAllFromSetAsync(string key)
        {
            lock (setLock)
            {
                IReadOnlyCollection<string> members = memoryCache.TryGetValue(key, out CacheEntry? entry) && entry is not null
                    ? JsonSerializer.Deserialize<List<string>>(entry.Json) ?? []
                    : [];
                memoryCache.Remove(key);
                return Task.FromResult(members);
            }
        }

        private readonly object setLock = new();
    }
}
