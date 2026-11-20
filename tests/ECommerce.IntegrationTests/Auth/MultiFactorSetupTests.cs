using System.Net;
using System.Net.Http.Json;
using ECommerce.IntegrationTests.Infrastructure;
using OtpNet;

namespace ECommerce.IntegrationTests.Auth
{
    // Spec 0002 — MFA kurulumu (AC-7). MFA oturumu hazırlıkta açılır; kurulum isteği HTTP ile.
    [Collection(IntegrationCollection.Name)]
    [Trait("Category", "Integration")]
    public class MultiFactorSetupTests(ApiFixture fixture)
    {
        private readonly AuthArrange arrange = new(fixture);

        [Fact]
        public async Task Setup_ValidSession_ReturnsSecretAndTotpUri()
        {
            string email = AuthArrange.NewEmail();
            Guid userId = await arrange.CreateUserAsync(email);
            string mfaToken = await arrange.OpenMfaSessionAsync(userId);
            using HttpClient client = arrange.CreateClient();

            HttpResponseMessage response = await client.PostAsJsonAsync(AuthArrange.MfaSetupPath, new { multiFactorToken = mfaToken }, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            SetupResult result = await AuthArrange.ReadJsonAsync<SetupResult>(response);

            // Gizli anahtar doğrulayıcı uygulamaya eklenebilir: Base32 ve boş değil.
            Assert.False(string.IsNullOrWhiteSpace(result.SecretKey));
            Assert.NotEmpty(Base32Encoding.ToBytes(result.SecretKey));

            Assert.NotNull(result.QrCodeUri);
            Assert.StartsWith("otpauth://totp/", result.QrCodeUri, StringComparison.Ordinal);
            string decodedUri = Uri.UnescapeDataString(result.QrCodeUri);
            Assert.Contains(email, decodedUri, StringComparison.Ordinal);
            Assert.Contains(ApiFixture.MultiFactorIssuer, decodedUri, StringComparison.Ordinal);
        }

        [Fact]
        public async Task Setup_UnknownSession_ReturnsProblem400()
        {
            using HttpClient client = arrange.CreateClient();
            string unknownToken = Convert.ToHexStringLower(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

            HttpResponseMessage response = await client.PostAsJsonAsync(AuthArrange.MfaSetupPath, new { multiFactorToken = unknownToken }, TestContext.Current.CancellationToken);

            await ProblemAssert.IsProblemAsync(response, HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Setup_MfaAlreadyVerified_ReturnsProblem400()
        {
            Guid userId = await arrange.CreateUserAsync(AuthArrange.NewEmail());
            using HttpClient client = arrange.CreateClient();
            await arrange.EnableMfaAsync(client, userId);
            string newMfaToken = await arrange.OpenMfaSessionAsync(userId);

            HttpResponseMessage response = await client.PostAsJsonAsync(AuthArrange.MfaSetupPath, new { multiFactorToken = newMfaToken }, TestContext.Current.CancellationToken);

            await ProblemAssert.IsProblemAsync(response, HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Setup_Repeated_OnlyLatestSecretAccepted()
        {
            Guid userId = await arrange.CreateUserAsync(AuthArrange.NewEmail());
            string mfaToken = await arrange.OpenMfaSessionAsync(userId);
            using HttpClient client = arrange.CreateClient();

            string firstSecret = await arrange.SetupMfaAsync(client, mfaToken);
            string latestSecret = await arrange.SetupMfaAsync(client, mfaToken);
            Assert.NotEqual(firstSecret, latestSecret);

            HttpResponseMessage oldSecretResponse = await AuthArrange.VerifyAsync(client, mfaToken, AuthArrange.CurrentCode(firstSecret));
            HttpResponseMessage latestSecretResponse = await AuthArrange.VerifyAsync(client, mfaToken, AuthArrange.CurrentCode(latestSecret));

            await ProblemAssert.IsProblemAsync(oldSecretResponse, HttpStatusCode.BadRequest);
            Assert.Equal(HttpStatusCode.OK, latestSecretResponse.StatusCode);
        }
    }
}
