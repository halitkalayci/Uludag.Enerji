using Microsoft.Extensions.Time.Testing;
using Uludag.Core.RateLimiting;
using Uludag.Core.RateLimiting.InMemory;

namespace Uludag.Core.Tests.RateLimiting
{
    // Leaky Bucket: kapasite kadar istek kabul edilir, fazlası reddedilir; her LeakInterval'da bir damla sızar.
    // Zaman FakeTimeProvider ile ilerletilir — testler gerçek saati beklemez (docs/testing.md, Determinism).
    public class InMemoryLeakyBucketRateLimiterTests
    {
        private const string ClientKey = "127.0.0.1";
        private const int Capacity = 2;
        private const int LeakIntervalSeconds = 10;

        private readonly FakeTimeProvider timeProvider = new();
        private readonly InMemoryLeakyBucketRateLimiter rateLimiter;

        public InMemoryLeakyBucketRateLimiterTests()
        {
            this.rateLimiter = new InMemoryLeakyBucketRateLimiter(
                new LeakyBucketOptions { Capacity = Capacity, LeakIntervalSeconds = LeakIntervalSeconds },
                timeProvider);
        }

        [Fact]
        public async Task TryAcquireAsync_WithinCapacity_IsAllowed()
        {
            for (int request = 1; request <= Capacity; request++)
            {
                RateLimitResult result = await rateLimiter.TryAcquireAsync(ClientKey);

                Assert.True(result.IsAllowed, $"Kapasite içindeki {request}. istek kabul edilmeliydi.");
            }
        }

        [Fact]
        public async Task TryAcquireAsync_OverCapacity_IsRejected()
        {
            await FillBucketAsync();

            RateLimitResult result = await rateLimiter.TryAcquireAsync(ClientKey);

            Assert.False(result.IsAllowed);
            Assert.True(result.RetryAfter > TimeSpan.Zero, "Reddedilen istek ne zaman tekrar denenebileceğini bildirmeli.");
            Assert.True(result.RetryAfter <= TimeSpan.FromSeconds(LeakIntervalSeconds));
        }

        [Fact]
        public async Task TryAcquireAsync_AfterLeakInterval_IsAllowedAgain()
        {
            await FillBucketAsync();
            Assert.False((await rateLimiter.TryAcquireAsync(ClientKey)).IsAllowed);

            timeProvider.Advance(TimeSpan.FromSeconds(LeakIntervalSeconds));

            RateLimitResult result = await rateLimiter.TryAcquireAsync(ClientKey);

            Assert.True(result.IsAllowed);
        }

        private async Task FillBucketAsync()
        {
            for (int request = 0; request < Capacity; request++)
                Assert.True((await rateLimiter.TryAcquireAsync(ClientKey)).IsAllowed);
        }
    }
}
