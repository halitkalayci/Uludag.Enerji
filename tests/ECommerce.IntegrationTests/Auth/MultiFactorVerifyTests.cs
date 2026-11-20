using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using ECommerce.Domain.Entities;
using ECommerce.IntegrationTests.Infrastructure;
using ECommerce.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.IntegrationTests.Auth
{
    // Spec 0002 — MFA doğrulama (AC-8, AC-9; BR-9, BR-10'un saklama kuralı).
    [Collection(IntegrationCollection.Name)]
    [Trait("Category", "Integration")]
    public class MultiFactorVerifyTests(ApiFixture fixture)
    {
        private readonly AuthArrange arrange = new(fixture);

        // AC-8
        [Fact]
        public async Task Verify_ValidCode_ReturnsTokensUsableForLogout()
        {
            Guid userId = await arrange.CreateUserAsync(AuthArrange.NewEmail());
            string mfaToken = await arrange.OpenMfaSessionAsync(userId);
            using HttpClient client = arrange.CreateClient();
            string secret = await arrange.SetupMfaAsync(client, mfaToken);

            HttpResponseMessage response = await AuthArrange.VerifyAsync(client, mfaToken, AuthArrange.CurrentCode(secret));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            TokenResult tokens = await AuthArrange.ReadJsonAsync<TokenResult>(response);
            Assert.False(string.IsNullOrWhiteSpace(tokens.AccessToken));
            Assert.False(string.IsNullOrWhiteSpace(tokens.RefreshToken));
            Assert.True(tokens.Expiration > DateTime.UtcNow, "Erişim token'ının bitişi gelecekte olmalı.");
            Assert.True(tokens.RefreshTokenExpiration > DateTime.UtcNow, "Refresh token'ın bitişi gelecekte olmalı.");

            // Kimlik doğrulama isteyen istek: token olmadan reddedilir, token ile geçer.
            HttpResponseMessage withoutToken = await client.PostAsync(AuthArrange.LogoutPath, null, TestContext.Current.CancellationToken);
            await ProblemAssert.IsProblemAsync(withoutToken, HttpStatusCode.Unauthorized);

            using HttpRequestMessage logout = new(HttpMethod.Post, AuthArrange.LogoutPath);
            logout.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
            HttpResponseMessage withToken = await client.SendAsync(logout, TestContext.Current.CancellationToken);
            Assert.True(withToken.IsSuccessStatusCode, $"Geçerli token ile çıkış başarısız: {(int)withToken.StatusCode}");
        }

        // AC-8 — MFA token'ı tek kullanımlıktır.
        [Fact]
        public async Task Verify_UsedMfaToken_ReturnsProblem400()
        {
            Guid userId = await arrange.CreateUserAsync(AuthArrange.NewEmail());
            string mfaToken = await arrange.OpenMfaSessionAsync(userId);
            using HttpClient client = arrange.CreateClient();
            string secret = await arrange.SetupMfaAsync(client, mfaToken);
            HttpResponseMessage first = await AuthArrange.VerifyAsync(client, mfaToken, AuthArrange.CurrentCode(secret));
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);

            // Farklı (sonraki adımın) geçerli koduyla: red sebebi kod tekrarı değil, token'ın kullanılmış olması.
            HttpResponseMessage second = await AuthArrange.VerifyAsync(client, mfaToken, AuthArrange.NextStepCode(secret));

            await ProblemAssert.IsProblemAsync(second, HttpStatusCode.BadRequest);
            Assert.DoesNotContain("accessToken", await second.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.OrdinalIgnoreCase);
        }

        // AC-8 — refresh token'ın kendisi değil, yalnızca SHA-256 özeti saklanır (BR-10).
        [Fact]
        public async Task Verify_ValidCode_StoresOnlyRefreshTokenSha256()
        {
            Guid userId = await arrange.CreateUserAsync(AuthArrange.NewEmail());
            using HttpClient client = arrange.CreateClient();
            (_, TokenResult tokens) = await arrange.EnableMfaAsync(client, userId);
            string refreshToken = tokens.RefreshToken!;

            // Özet testte bağımsız hesaplanır.
            string expectedHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));

            await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
            UludagDbContext dbContext = scope.ServiceProvider.GetRequiredService<UludagDbContext>();
            List<RefreshToken> stored = await dbContext.RefreshTokens.AsNoTracking()
                .Where(r => r.UserId == userId)
                .ToListAsync(TestContext.Current.CancellationToken);

            RefreshToken row = Assert.Single(stored);
            Assert.Equal(expectedHash, row.TokenHash, ignoreCase: true);
            Assert.DoesNotContain(stored, r => r.TokenHash.Contains(refreshToken, StringComparison.OrdinalIgnoreCase));
        }

        // AC-8 — doğrulamadan sonra MFA "kurulu" sayılır: sonraki giriş kod ister.
        [Fact]
        public async Task Verify_ValidCode_NextLoginRequiresCode()
        {
            string email = AuthArrange.NewEmail();
            Guid userId = await arrange.CreateUserAsync(email);
            using HttpClient client = arrange.CreateClient();
            await arrange.EnableMfaAsync(client, userId);

            HttpResponseMessage response = await AuthArrange.LoginAsync(client, email, AuthArrange.Password);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            LoginResult result = await AuthArrange.ReadJsonAsync<LoginResult>(response);
            Assert.Equal("CodeRequired", result.MultiFactorStatus);
        }

        // AC-9
        [Fact]
        public async Task Verify_WrongCode_ReturnsProblem400()
        {
            Guid userId = await arrange.CreateUserAsync(AuthArrange.NewEmail());
            string mfaToken = await arrange.OpenMfaSessionAsync(userId);
            using HttpClient client = arrange.CreateClient();
            string secret = await arrange.SetupMfaAsync(client, mfaToken);

            HttpResponseMessage response = await AuthArrange.VerifyAsync(client, mfaToken, AuthArrange.WrongCode(secret));

            await ProblemAssert.IsProblemAsync(response, HttpStatusCode.BadRequest);
            Assert.DoesNotContain("accessToken", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.OrdinalIgnoreCase);
        }

        // AC-9 — 6 haneli rakam olmayan kod.
        [Theory]
        [InlineData("12345")]
        [InlineData("1234567")]
        [InlineData("abcdef")]
        [InlineData("")]
        public async Task Verify_MalformedCode_ReturnsProblem400(string malformedCode)
        {
            Guid userId = await arrange.CreateUserAsync(AuthArrange.NewEmail());
            string mfaToken = await arrange.OpenMfaSessionAsync(userId);
            using HttpClient client = arrange.CreateClient();
            await arrange.SetupMfaAsync(client, mfaToken);

            HttpResponseMessage response = await AuthArrange.VerifyAsync(client, mfaToken, malformedCode);

            await ProblemAssert.IsProblemAsync(response, HttpStatusCode.BadRequest);
            Assert.DoesNotContain("accessToken", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.OrdinalIgnoreCase);
        }

        // AC-9 — başarıyla kullanılmış geçerli kod tekrar kabul edilmez (RFC 6238 §5.2).
        [Fact]
        public async Task Verify_ReplayedCode_ReturnsProblem400()
        {
            Guid userId = await arrange.CreateUserAsync(AuthArrange.NewEmail());
            string firstMfaToken = await arrange.OpenMfaSessionAsync(userId);
            using HttpClient client = arrange.CreateClient();
            string secret = await arrange.SetupMfaAsync(client, firstMfaToken);
            string usedCode = AuthArrange.CurrentCode(secret);
            HttpResponseMessage first = await AuthArrange.VerifyAsync(client, firstMfaToken, usedCode);
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);

            // Yeni bir MFA oturumunda (yeni giriş), aynı kod yeniden gönderilir.
            string secondMfaToken = await arrange.OpenMfaSessionAsync(userId);
            HttpResponseMessage replay = await AuthArrange.VerifyAsync(client, secondMfaToken, usedCode);

            await ProblemAssert.IsProblemAsync(replay, HttpStatusCode.BadRequest);
            Assert.DoesNotContain("accessToken", await replay.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.OrdinalIgnoreCase);
        }
    }
}
