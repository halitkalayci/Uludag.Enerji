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
        // Girişin kendisi bu testin konusu: ikinci MFA oturumu gerçek HTTP girişiyle (yalnızca e-posta + şifre) açılır.
        // Giriş sayacı sıfırlasaydı her girişte 5 yeni deneme hakkı doğar, BR-9 delinirdi.
        [Fact]
        public async Task Verify_AttemptsAfterNewLogin_NotReset()
        {
            string email = AuthArrange.NewEmail();
            Guid userId = await arrange.CreateUserAsync(email);
            string firstSession = await arrange.OpenMfaSessionAsync(userId);
            using HttpClient client = arrange.CreateClient();
            string secret = await arrange.SetupMfaAsync(client, firstSession);
            await FailAttemptsAsync(client, firstSession, secret, 3);

            HttpResponseMessage login = await AuthArrange.LoginAsync(client, email, AuthArrange.Password);
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            string secondSession = (await AuthArrange.ReadJsonAsync<LoginResult>(login)).MultiFactorToken!;
            await FailAttemptsAsync(client, secondSession, secret, 2);

            HttpResponseMessage sixth = await AuthArrange.VerifyAsync(client, secondSession, AuthArrange.CurrentCode(secret));

            await ProblemAssert.IsProblemAsync(sixth, HttpStatusCode.BadRequest);
            Assert.DoesNotContain("accessToken", await sixth.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.OrdinalIgnoreCase);
        }

        // BR-9: kilit limit AŞILINCA başlar ve 15 dk sürer. Hatalı denemeler zamana yayılmış olabilir: ilk denemeden
        // sonra "13 dk geçmiş" gibi davranılır (deneme kaydının kalan ömrü Redis'te 2 dk'ya indirilir — plan A-1, R-2).
        // Kilit ilk denemeden başlıyorsa bu noktada ~2 dk kalır; doğru uygulamada aşıldığı andan itibaren ~15 dk.
        [Fact]
        public async Task Verify_Lockout_LastsFifteenMinutes()
        {
            Guid userId = await arrange.CreateUserAsync(AuthArrange.NewEmail());
            string mfaToken = await arrange.OpenMfaSessionAsync(userId);
            using HttpClient client = arrange.CreateClient();
            string secret = await arrange.SetupMfaAsync(client, mfaToken);

            // Anahtar adı koda sabitlenmez: bu kullanıcıya ait kayıtlar Redis'te kullanıcı id'siyle aranır.
            await using ConnectionMultiplexer redis = await ConnectionMultiplexer.ConnectAsync(fixture.RedisConnectionString);
            IServer server = redis.GetServer(redis.GetEndPoints().Single());
            IDatabase database = redis.GetDatabase();
            HashSet<RedisKey> keysBeforeAttempts = await KeysOfUserAsync(server, userId);

            await FailAttemptsAsync(client, mfaToken, secret, 1);
            List<RedisKey> attemptKeys = (await KeysOfUserAsync(server, userId)).Except(keysBeforeAttempts).ToList();
            Assert.NotEmpty(attemptKeys);
            TimeSpan remainingAfterTimePassed = TimeSpan.FromMinutes(2);
            foreach (RedisKey key in attemptKeys)
                Assert.True(await database.KeyExpireAsync(key, remainingAfterTimePassed), $"Deneme kaydının süresi ayarlanamadı: {key}");

            await FailAttemptsAsync(client, mfaToken, secret, MaxAttempts - 1);
            HttpResponseMessage locked = await AuthArrange.VerifyAsync(client, mfaToken, AuthArrange.CurrentCode(secret));
            await ProblemAssert.IsProblemAsync(locked, HttpStatusCode.BadRequest);

            List<TimeSpan> remainingLifetimes = [];
            foreach (RedisKey key in (await KeysOfUserAsync(server, userId)).Except(keysBeforeAttempts))
            {
                TimeSpan? ttl = await database.KeyTimeToLiveAsync(key);
                if (ttl is not null)
                    remainingLifetimes.Add(ttl.Value);
            }

            TimeSpan tolerance = TimeSpan.FromMinutes(1);
            Assert.True(remainingLifetimes.Any(ttl => ttl <= LockoutDuration && ttl > LockoutDuration - tolerance),
                $"Limit aşıldıktan sonra ~15 dk kilit bekleniyordu; kullanıcının kayıtlarında kalan süreler: [{string.Join(", ", remainingLifetimes)}]");
        }

        private static async Task<HashSet<RedisKey>> KeysOfUserAsync(IServer server, Guid userId)
        {
            HashSet<RedisKey> keys = [];
            await foreach (RedisKey key in server.KeysAsync(pattern: $"*{userId}*"))
                keys.Add(key);
            return keys;
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
