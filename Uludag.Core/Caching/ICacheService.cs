namespace Uludag.Core.Caching
{
    // Anahtar -> değer saklama sözleşmesi. Kullanan kod verinin nerede durduğunu (Redis / bellek) bilmez.
    // Değerler JSON olarak saklanır; her iki implementasyon da aynı davranır (okunan değer, saklananın kopyasıdır).
    public interface ICacheService
    {
        // Anahtar yoksa veya süresi dolmuşsa default (referans tiplerde null).
        Task<T?> GetAsync<T>(string key);

        // expiration -> anahtar bu süre sonunda kendiliğinden silinir.
        Task SetAsync<T>(string key, T value, TimeSpan expiration);

        // Yalnızca anahtar zaten varsa değerini değiştirir; kalan süresine (TTL) dokunmaz.
        // Anahtar yoksa hiçbir şey yazmaz ve false döner.
        Task<bool> SetIfExistsAsync<T>(string key, T value);

        // Sayaç: anahtarın değerini atomik olarak 1 artırır ve yeni değeri döner.
        // Anahtar yoksa 1 ile oluşturulur ve süresi (expiration) o an başlar; sonraki artışlar süreyi uzatmaz (sabit pencere).
        // Atomik -> aynı anda gelen iki istek aynı değeri göremez (ör. deneme hakkı sayımı).
        Task<long> IncrementAsync(string key, TimeSpan expiration);

        Task RemoveAsync(string key);
    }
}
