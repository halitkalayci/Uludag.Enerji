namespace Uludag.Core.RateLimiting
{
    // Leaky Bucket (meter yorumu) sözleşmesi. Kullanan kod kovaların nerede durduğunu (Redis / bellek) bilmez.
    // Algoritma (her çağrıda, atomik):
    //   1. Son sızmadan bu yana geçen tam aralık sayısı kadar damla sızar (kova negatife düşmez).
    //   2. Kova doluysa -> reddedilir, bir sonraki damlaya kalan süre döner.
    //   3. Değilse -> bir damla eklenir, istek geçer.
    // Kova boşken eklenen ilk damlanın sızma saati o an başlar.
    public interface ILeakyBucketRateLimiter
    {
        // key -> kovanın anahtarı (ör. istemci IP'si). Aynı anahtarı kullanan istekler aynı kovayı paylaşır.
        Task<RateLimitResult> TryAcquireAsync(string key);
    }
}
