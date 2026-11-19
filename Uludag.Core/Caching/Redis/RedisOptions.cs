namespace Uludag.Core.Caching.Redis
{
    // appsettings'teki "RedisOptions" bölümünün C# karşılığı.
    public class RedisOptions
    {
        // StackExchange.Redis bağlantı metni. Ör: "127.0.0.1:6379,password=redis".
        // Boş bırakılırsa Redis kullanılmaz, sistem in-memory çalışır (bkz. AddCacheService).
        public string? ConnectionString { get; set; }

        // Tüm anahtarların önüne eklenir. Aynı Redis'i paylaşan uygulamaların anahtarları çakışmaz.
        // Ör: "uludag:" + "token-session:abc" -> "uludag:token-session:abc".
        public string InstanceName { get; set; } = "uludag:";
    }
}
