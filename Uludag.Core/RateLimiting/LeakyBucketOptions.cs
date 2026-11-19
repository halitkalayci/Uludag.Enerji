namespace Uludag.Core.RateLimiting
{
    // appsettings'teki "RateLimitOptions" bölümünün C# karşılığı.
    // Leaky Bucket: her istemcinin bir kovası vardır; her istek kovaya bir damla ekler,
    // kova LeakIntervalSeconds'ta bir damla sızdırır. Kova doluyken gelen istek reddedilir (429).
    public class LeakyBucketOptions
    {
        // Kovanın alabileceği en fazla damla (istek) sayısı. Boş kovayla art arda bu kadar istek geçer.
        public int Capacity { get; set; } = 10;

        // Kaç saniyede bir damla sızar. Kalıcı hız -> 60 / LeakIntervalSeconds istek / dakika (12 sn -> 5 istek/dk).
        public int LeakIntervalSeconds { get; set; } = 12;
    }
}
