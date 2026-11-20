using System.Security.Cryptography;
using Uludag.Core.Caching;

namespace Uludag.Core.Security.MultiFactor
{
    // IMultiFactorSessionHelper'ın ICacheService (Redis / in-memory) ile yazılmış hali.
    // Cache anahtarları:
    //   multi-factor-session:{token}           -> UserId, Expiration. TTL = SessionExpiration.
    //   multi-factor-attempts:{userId}         -> Deneme sayacı. TTL = LockoutDuration; ilk denemede başlar ve limit
    //                                             dolduğunda yeniden kurulur (kilit limitten itibaren tam süre, BR-9).
    public class MultiFactorSessionHelper : IMultiFactorSessionHelper
    {
        private const string SessionCacheKeyPrefix = "multi-factor-session:";
        private const string AttemptsCacheKeyPrefix = "multi-factor-attempts:";

        // 256 bit rastgele -> tahmin edilemez.
        private const int TokenByteLength = 32;

        private sealed record StoredMultiFactorSession(Guid UserId, DateTime Expiration);

        private readonly MultiFactorOptions multiFactorOptions;
        private readonly ICacheService cacheService;
        public MultiFactorSessionHelper(MultiFactorOptions multiFactorOptions, ICacheService cacheService)
        {
            this.multiFactorOptions = multiFactorOptions;
            this.cacheService = cacheService;
        }

        public async Task<MultiFactorSession> CreateSessionAsync(Guid userId)
        {
            DateTime now = DateTime.UtcNow;
            DateTime expiration = now.AddMinutes(multiFactorOptions.SessionExpiration);

            // Hex -> URL / JSON içinde kaçış gerektirmez.
            string token = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(TokenByteLength));

            await cacheService.SetAsync(BuildSessionCacheKey(token), new StoredMultiFactorSession(userId, expiration), expiration - now);

            return new MultiFactorSession
            {
                Token = token,
                UserId = userId,
                Expiration = expiration
            };
        }

        public async Task<MultiFactorSession?> GetSessionAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            StoredMultiFactorSession? storedSession = await cacheService.GetAsync<StoredMultiFactorSession>(BuildSessionCacheKey(token));
            if (storedSession is null)
                return null;

            return new MultiFactorSession
            {
                Token = token,
                UserId = storedSession.UserId,
                Expiration = storedSession.Expiration
            };
        }

        public async Task RemoveSessionAsync(string token)
        {
            await cacheService.RemoveAsync(BuildSessionCacheKey(token));
        }

        public async Task<bool> TryConsumeVerificationAttemptAsync(Guid userId)
        {
            // Sayaç kullanıcı başınadır (oturum başına değil): yeniden login olup yeni token almak hakları sıfırlamaz.
            string attemptsKey = BuildAttemptsCacheKey(userId);
            TimeSpan lockoutDuration = TimeSpan.FromMinutes(multiFactorOptions.LockoutDuration);
            long attempt = await cacheService.IncrementAsync(attemptsKey, lockoutDuration);

            // Limit tam bu denemede doldu: kilit şimdi başlar ve LockoutDuration boyunca sürer. Denemeler zamana yayılmış
            // olsa bile kilit, ilk denemeden kalan süre kadar değil, tam süre geçerlidir (BR-9).
            // Not: artırma ile yeniden yazma ayrı komutlardır; tam bu anda eşzamanlı bir deneme en fazla bir ek hak
            // kazanabilir (backlog B-9).
            if (attempt == multiFactorOptions.MaxVerificationAttempts)
                await cacheService.SetAsync(attemptsKey, attempt, lockoutDuration);

            return attempt <= multiFactorOptions.MaxVerificationAttempts;
        }

        public async Task ResetVerificationAttemptsAsync(Guid userId)
        {
            await cacheService.RemoveAsync(BuildAttemptsCacheKey(userId));
        }

        private static string BuildSessionCacheKey(string token) => SessionCacheKeyPrefix + token;

        private static string BuildAttemptsCacheKey(Guid userId) => AttemptsCacheKeyPrefix + userId;
    }
}
