namespace Uludag.Core.RateLimiting
{
    // Kovaya damla ekleme denemesinin sonucu.
    //   IsAllowed  -> true: damla eklendi, istek geçebilir. false: kova dolu, istek reddedilmeli.
    //   RetryAfter -> reddedildiyse bir sonraki damlanın sızmasına kalan süre (WebAPI'de Retry-After başlığı).
    public sealed record RateLimitResult(bool IsAllowed, TimeSpan RetryAfter)
    {
        public static RateLimitResult Allowed { get; } = new(true, TimeSpan.Zero);

        public static RateLimitResult Rejected(TimeSpan retryAfter) => new(false, retryAfter);
    }
}
