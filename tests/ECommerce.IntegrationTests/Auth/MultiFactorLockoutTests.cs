using System.Net;
using ECommerce.IntegrationTests.Infrastructure;
using StackExchange.Redis;

namespace ECommerce.IntegrationTests.Auth
{
    // Spec 0002 — BR-9 hatalı kod kilidi (AC-10): en fazla 5 hatalı deneme, sonra 15 dk kilit.
    // Denemeler kullanıcı başına sayılır; yeni MFA oturumu (yeni giriş) sayacı sıfırlamaz.
    [Collection(IntegrationCollection.Name)]
    [Trait("Category", "Integration")]
    public class MultiFactorLockoutTests(ApiFixture fixture)
    {
        private const int MaxAttempts = 5;
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

        private readonly AuthArrange arrange = new(fixture);

        [Fact]
        public async Task Verify_SixthAttemptAcrossSessions_RejectedEvenWithValidCode()
        {
            Guid userId = await arrange.CreateUserAsync(AuthArrange.NewEmail());
            string firstSession = await arrange.OpenMfaSessionAsync(userId);
            using HttpClient client = arrange.CreateClient();
            string secret = await arrange.SetupMfaAsync(client, firstSession);

            // Bir oturumda 3, yeni bir girişle açılan diğerinde 2 hatalı deneme = toplam 5.
            await FailAttemptsAsync(client, firstSession, secret, 3);
            string secondSession = await arrange.OpenMfaSessionAsync(userId);
            await FailAttemptsAsync(client, secondSession, secret, 2);

            HttpResponseMessage sixth = await AuthArrange.VerifyAsync(client, secondSession, AuthArrange.CurrentCode(secret));

            await ProblemAssert.IsProblemAsync(sixth, HttpStatusCode.BadRequest);
            Assert.DoesNotContain("accessToken", await sixth.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.OrdinalIgnoreCase);
        }

        // Kilidin süresi beklenmeden ölçülür (spec Constraints; plan R-2): kullanıcının kilit kaydının kalan ömrü.
        [Fact]
        public async Task Verify_Lockout_LastsFifteenMinutes()
        {
            Guid userId = await arrange.CreateUserAsync(AuthArrange.NewEmail());
            string mfaToken = await arrange.OpenMfaSessionAsync(userId);
            using HttpClient client = arrange.CreateClient();
            string secret = await arrange.SetupMfaAsync(client, mfaToken);
            await FailAttemptsAsync(client, mfaToken, secret, MaxAttempts);
            HttpResponseMessage locked = await AuthArrange.VerifyAsync(client, mfaToken, AuthArrange.CurrentCode(secret));
            await ProblemAssert.IsProblemAsync(locked, HttpStatusCode.BadRequest);

            // Anahtar adı koda sabitlenmez: Redis'te bu kullanıcıya ait, süreli kayıtlar aranır.
            await using ConnectionMultiplexer redis = await ConnectionMultiplexer.ConnectAsync(fixture.RedisConnectionString);
            IServer server = redis.GetServer(redis.GetEndPoints().Single());
            IDatabase database = redis.GetDatabase();
            List<TimeSpan> remainingLifetimes = [];
            await foreach (RedisKey key in server.KeysAsync(pattern: $"*{userId}*"))
            {
                TimeSpan? ttl = await database.KeyTimeToLiveAsync(key);
                if (ttl is not null)
                    remainingLifetimes.Add(ttl.Value);
            }

            TimeSpan tolerance = TimeSpan.FromMinutes(1);
            Assert.Contains(remainingLifetimes, ttl => ttl <= LockoutDuration && ttl > LockoutDuration - tolerance);
        }

        [Fact]
        public async Task Verify_SuccessBeforeLimit_ResetsAttempts()
        {
            Guid userId = await arrange.CreateUserAsync(AuthArrange.NewEmail());
            string firstSession = await arrange.OpenMfaSessionAsync(userId);
            using HttpClient client = arrange.CreateClient();
            string secret = await arrange.SetupMfaAsync(client, firstSession);

            await FailAttemptsAsync(client, firstSession, secret, MaxAttempts - 1);
            HttpResponseMessage firstSuccess = await AuthArrange.VerifyAsync(client, firstSession, AuthArrange.CurrentCode(secret));
            Assert.Equal(HttpStatusCode.OK, firstSuccess.StatusCode);

            // Sayaç sıfırlanmasaydı buradaki denemeler toplamda 5'i aşar ve geçerli kod reddedilirdi.
            string secondSession = await arrange.OpenMfaSessionAsync(userId);
            await FailAttemptsAsync(client, secondSession, secret, MaxAttempts - 1);
            HttpResponseMessage secondSuccess = await AuthArrange.VerifyAsync(client, secondSession, AuthArrange.NextStepCode(secret));

            Assert.Equal(HttpStatusCode.OK, secondSuccess.StatusCode);
        }

        private static async Task FailAttemptsAsync(HttpClient client, string mfaToken, string secret, int count)
        {
            for (int attempt = 0; attempt < count; attempt++)
            {
                HttpResponseMessage response = await AuthArrange.VerifyAsync(client, mfaToken, AuthArrange.WrongCode(secret));
                await ProblemAssert.IsProblemAsync(response, HttpStatusCode.BadRequest);
            }
        }
    }
}
