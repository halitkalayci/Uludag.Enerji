using System.Collections.Concurrent;

namespace Uludag.Core.RateLimiting.InMemory
{
    // ILeakyBucketRateLimiter'ın uygulama belleğinde çalışan hali. Redis yokken (bağlantı ayarı boş) kullanılır.
    // Dikkat: kovalar uygulama kapanınca sıfırlanır ve birden fazla instance arasında paylaşılmaz
    // (iki instance varsa her istemci fiilen iki kat hak kazanır).
    public class InMemoryLeakyBucketRateLimiter : ILeakyBucketRateLimiter
    {
        // Bir anahtarın kovası. Alanlar yalnızca kovanın kilidi altında okunur / yazılır.
        private sealed class Bucket
        {
            // Kovadaki damla sayısı (0..Capacity).
            public long Level;
            // Son sızmanın zamanı (Unix ms). Sızmalar tam aralıklarla ilerler: LastLeak += damla * aralık.
            public long LastLeak;
            // Temizlikte sözlükten çıkarıldı. Bu kovayı o an elinde tutan istek yeni kovayla yeniden dener,
            // böylece sözlükte olmayan bir kovaya yazılıp kaybolan damla olmaz.
            public bool Removed;
        }

        private readonly ConcurrentDictionary<string, Bucket> buckets = new();
        private readonly int capacity;
        private readonly long leakIntervalMilliseconds;
        private readonly TimeProvider timeProvider;

        // Boş kovaların temizleneceği bir sonraki an (Unix ms). Temizlik en fazla "Capacity * aralık"ta bir yapılır:
        // dolu bir kova en geç bu sürede boşalır, sözlük ziyaret edilen IP'lerle sınırsız büyümez.
        private long nextSweepAt;

        public InMemoryLeakyBucketRateLimiter(LeakyBucketOptions leakyBucketOptions, TimeProvider timeProvider)
        {
            capacity = leakyBucketOptions.Capacity;
            leakIntervalMilliseconds = leakyBucketOptions.LeakIntervalSeconds * 1000L;
            this.timeProvider = timeProvider;
        }

        public Task<RateLimitResult> TryAcquireAsync(string key)
        {
            SweepIfDue();

            while (true)
            {
                Bucket bucket = buckets.GetOrAdd(key, _ => new Bucket());
                lock (bucket)
                {
                    // Temizlik bu kovayı az önce sözlükten çıkardı -> sözlükteki (yeni) kovayla tekrar dene.
                    if (bucket.Removed)
                        continue;

                    // Zaman kilidin içinde alınır: kilidi bekleyen istek, kendinden sonra gelenin zamanını geriye almaz.
                    long now = timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
                    Leak(bucket, now);

                    if (bucket.Level + 1 > capacity)
                        return Task.FromResult(RateLimitResult.Rejected(TimeSpan.FromMilliseconds(bucket.LastLeak + leakIntervalMilliseconds - now)));

                    bucket.Level++;
                    return Task.FromResult(RateLimitResult.Allowed);
                }
            }
        }

        // Son sızmadan bu yana geçen tam aralıklar kadar damla sızdırır.
        // Kova boşsa sızma saati şimdiye çekilir: bir sonraki damla tam bir aralık sonra sızar (erken sızma olmaz).
        private void Leak(Bucket bucket, long now)
        {
            long drips = (now - bucket.LastLeak) / leakIntervalMilliseconds;
            if (drips > 0)
            {
                bucket.Level = Math.Max(0, bucket.Level - drips);
                bucket.LastLeak += drips * leakIntervalMilliseconds;
            }

            if (bucket.Level == 0)
                bucket.LastLeak = now;
        }

        private void SweepIfDue()
        {
            long now = timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
            long sweepAt = Interlocked.Read(ref nextSweepAt);
            // Zamanı gelmediyse ya da başka bir istek temizliği üstlendiyse -> atla.
            if (now < sweepAt || Interlocked.CompareExchange(ref nextSweepAt, now + capacity * leakIntervalMilliseconds, sweepAt) != sweepAt)
                return;

            foreach (KeyValuePair<string, Bucket> entry in buckets)
            {
                lock (entry.Value)
                {
                    Leak(entry.Value, now);
                    if (entry.Value.Level > 0)
                        continue;

                    entry.Value.Removed = true;
                    // Yalnızca sözlükte hâlâ bu kova duruyorsa çıkarılır.
                    buckets.TryRemove(entry);
                }
            }
        }
    }
}
