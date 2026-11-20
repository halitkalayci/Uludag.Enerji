using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Contexts;
using Microsoft.Extensions.DependencyInjection;
using OtpNet;
using Uludag.Core.Security.Hashing;
using Uludag.Core.Security.MultiFactor;

namespace ECommerce.IntegrationTests.Infrastructure
{
    // Auth testlerinin HAZIRLIK adımları (spec 0002, R-3). Testin konusu olan adım her zaman testin kendisinde,
    // gerçek HTTP sözleşmesiyle yapılır; burada yalnızca "bir kullanıcı var", "açık bir MFA oturumu var" gibi
    // ön koşullar kurulur — böylece bir akıştaki hata (ör. giriş) diğer akışların testlerini maskelemez.
    public sealed class AuthArrange(ApiFixture fixture)
    {
        public const string Password = "Uludag-Test-Password-1!";

        // Sözleşmedeki uç noktalar (istemcinin göreceği yollar).
        public const string RegisterPath = "/api/auth/register";
        public const string LoginPath = "/api/auth/login";
        public const string MfaSetupPath = "/api/auth/mfa/setup";
        public const string MfaVerifyPath = "/api/auth/mfa/verify";
        public const string LogoutPath = "/api/auth/logout";

        // TOTP: RFC 6238 varsayılanları (SHA-1, 6 hane, 30 sn) — doğrulayıcı uygulamaların kullandığı biçim.
        private const int TotpStepSeconds = 30;
        private const int TotpDigits = 6;

        public HttpClient CreateClient() => fixture.Factory.CreateClient();

        public static string NewEmail() => $"user-{Guid.NewGuid():N}@uludag.test";

        // Kullanıcı uygulamanın kendi parola hash'leme yardımcısıyla doğrudan veritabanına yazılır.
        public async Task<Guid> CreateUserAsync(string email, string password = Password)
        {
            HashingHelper.CreatePasswordHash(password, out byte[] passwordHash, out byte[] passwordSalt);
            User user = new()
            {
                Id = Guid.NewGuid(),
                Email = email,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt
            };

            await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
            UludagDbContext dbContext = scope.ServiceProvider.GetRequiredService<UludagDbContext>();
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
            return user.Id;
        }

        // Girişin yerine geçen hazırlık: uygulamanın kendi MFA oturum servisiyle bir MFA token'ı açılır.
        public async Task<string> OpenMfaSessionAsync(Guid userId)
        {
            IMultiFactorSessionHelper sessionHelper = fixture.Factory.Services.GetRequiredService<IMultiFactorSessionHelper>();
            MultiFactorSession session = await sessionHelper.CreateSessionAsync(userId);
            return session.Token;
        }

        // MFA kurulumu (HTTP) — gizli anahtarı (Base32) döner.
        public async Task<string> SetupMfaAsync(HttpClient client, string mfaToken)
        {
            HttpResponseMessage response = await client.PostAsJsonAsync(MfaSetupPath, new { multiFactorToken = mfaToken }, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            SetupResult result = await ReadJsonAsync<SetupResult>(response);
            return result.SecretKey!;
        }

        // Kullanıcının MFA'sını uçtan uca kurar (oturum → setup → verify) ve verilen token'ları döner.
        public async Task<(string Secret, TokenResult Tokens)> EnableMfaAsync(HttpClient client, Guid userId)
        {
            string mfaToken = await OpenMfaSessionAsync(userId);
            string secret = await SetupMfaAsync(client, mfaToken);
            HttpResponseMessage response = await VerifyAsync(client, mfaToken, CurrentCode(secret));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (secret, await ReadJsonAsync<TokenResult>(response));
        }

        public static Task<HttpResponseMessage> VerifyAsync(HttpClient client, string mfaToken, string code) =>
            client.PostAsJsonAsync(MfaVerifyPath, new { multiFactorToken = mfaToken, authenticatorCode = code }, TestContext.Current.CancellationToken);

        public static Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password) =>
            client.PostAsJsonAsync(LoginPath, new { email, password }, TestContext.Current.CancellationToken);

        // TOTP kodları testte bağımsız üretilir (uygulamanın doğrulayıcısı kullanılmaz).
        public static string CurrentCode(string secret) => CodeAt(secret, DateTime.UtcNow);

        // Bir sonraki zaman adımının kodu: aynı adımda ikinci başarılı doğrulama gereken testlerde replay kuralıyla
        // çakışmamak için kullanılır (sunucunun ±1 adım penceresi içindedir).
        public static string NextStepCode(string secret) => CodeAt(secret, DateTime.UtcNow.AddSeconds(TotpStepSeconds));

        // Biçimi doğru ama penceredeki (ve bir adım ötesindeki) hiçbir geçerli koda eşit olmayan kod.
        public static string WrongCode(string secret)
        {
            DateTime now = DateTime.UtcNow;
            HashSet<string> validCodes = Enumerable.Range(-2, 5)
                .Select(step => CodeAt(secret, now.AddSeconds(step * TotpStepSeconds)))
                .ToHashSet();
            return Enumerable.Range(0, 10)
                .Select(digit => new string((char)('0' + digit), TotpDigits))
                .First(candidate => !validCodes.Contains(candidate));
        }

        private static string CodeAt(string secret, DateTime utc) =>
            new Totp(Base32Encoding.ToBytes(secret), TotpStepSeconds, OtpHashMode.Sha1, TotpDigits).ComputeTotp(utc);

        public static async Task<T> ReadJsonAsync<T>(HttpResponseMessage response)
        {
            T? value = await response.Content.ReadFromJsonAsync<T>(JsonSerializerOptions.Web, TestContext.Current.CancellationToken);
            Assert.NotNull(value);
            return value;
        }
    }

    // Sözleşmenin istemcinin göreceği şekli (uygulamanın response sınıfları kullanılmaz).
    public sealed record LoginResult(string? MultiFactorStatus, string? MultiFactorToken, DateTime? MultiFactorTokenExpiration, string? AccessToken);

    public sealed record SetupResult(string? SecretKey, string? QrCodeUri);

    public sealed record TokenResult(string? AccessToken, DateTime? Expiration, string? RefreshToken, DateTime? RefreshTokenExpiration);

    public static class ProblemAssert
    {
        // Hata yanıtı sözleşmesi (docs/security.md): beklenen durum kodu + ProblemDetails gövdesi.
        public static async Task<JsonElement> IsProblemAsync(HttpResponseMessage response, HttpStatusCode expectedStatus)
        {
            Assert.Equal(expectedStatus, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

            JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
            Assert.Equal((int)expectedStatus, body.GetProperty("status").GetInt32());
            Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("title").GetString()));
            return body;
        }
    }
}
