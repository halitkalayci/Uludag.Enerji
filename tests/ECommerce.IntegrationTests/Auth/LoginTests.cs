using System.Net;
using System.Text.Json;
using ECommerce.IntegrationTests.Infrastructure;

namespace ECommerce.IntegrationTests.Auth
{
    // Spec 0002 — giriş (AC-5, AC-6, BR-8). Giriş isteği sözleşmedeki gibi YALNIZCA e-posta + şifre taşır.
    [Collection(IntegrationCollection.Name)]
    [Trait("Category", "Integration")]
    public class LoginTests(ApiFixture fixture)
    {
        private readonly AuthArrange arrange = new(fixture);

        // AC-5 (MFA kurulmamış kullanıcı)
        [Fact]
        public async Task Login_ValidCredentialsWithoutMfa_ReturnsSetupRequiredMfaToken()
        {
            string email = AuthArrange.NewEmail();
            await arrange.CreateUserAsync(email);
            using HttpClient client = arrange.CreateClient();

            HttpResponseMessage response = await AuthArrange.LoginAsync(client, email, AuthArrange.Password);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            LoginResult result = await AuthArrange.ReadJsonAsync<LoginResult>(response);
            Assert.Equal("SetupRequired", result.MultiFactorStatus);
            Assert.False(string.IsNullOrWhiteSpace(result.MultiFactorToken));
            Assert.NotNull(result.MultiFactorTokenExpiration);
            Assert.Equal(DateTimeKind.Utc, result.MultiFactorTokenExpiration.Value.Kind);
            Assert.True(result.MultiFactorTokenExpiration.Value > DateTime.UtcNow, "MFA token'ının süresi gelecekte bitmeli.");
            Assert.Null(result.AccessToken);
        }

        // AC-5 (MFA kurulu kullanıcı)
        [Fact]
        public async Task Login_ValidCredentialsWithMfa_ReturnsCodeRequired()
        {
            string email = AuthArrange.NewEmail();
            Guid userId = await arrange.CreateUserAsync(email);
            using HttpClient client = arrange.CreateClient();
            await arrange.EnableMfaAsync(client, userId);

            HttpResponseMessage response = await AuthArrange.LoginAsync(client, email, AuthArrange.Password);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            LoginResult result = await AuthArrange.ReadJsonAsync<LoginResult>(response);
            Assert.Equal("CodeRequired", result.MultiFactorStatus);
            Assert.False(string.IsNullOrWhiteSpace(result.MultiFactorToken));
            Assert.NotNull(result.MultiFactorTokenExpiration);
            Assert.Equal(DateTimeKind.Utc, result.MultiFactorTokenExpiration.Value.Kind);
            Assert.True(result.MultiFactorTokenExpiration.Value > DateTime.UtcNow, "MFA token'ının süresi gelecekte bitmeli.");
            Assert.Null(result.AccessToken);
        }

        // AC-6 — yanıttan kullanıcının var olup olmadığı anlaşılamaz.
        [Fact]
        public async Task Login_UnknownEmailAndWrongPassword_ReturnIdenticalProblem400()
        {
            string registeredEmail = AuthArrange.NewEmail();
            await arrange.CreateUserAsync(registeredEmail);
            using HttpClient client = arrange.CreateClient();

            HttpResponseMessage unknownEmailResponse = await AuthArrange.LoginAsync(client, AuthArrange.NewEmail(), AuthArrange.Password);
            HttpResponseMessage wrongPasswordResponse = await AuthArrange.LoginAsync(client, registeredEmail, "Wrong-Password-9!");

            JsonElement unknownEmail = await ProblemAssert.IsProblemAsync(unknownEmailResponse, HttpStatusCode.BadRequest);
            JsonElement wrongPassword = await ProblemAssert.IsProblemAsync(wrongPasswordResponse, HttpStatusCode.BadRequest);

            Assert.Equal(unknownEmail.GetProperty("title").GetString(), wrongPassword.GetProperty("title").GetString());
            Assert.Equal(unknownEmail.GetProperty("detail").GetString(), wrongPassword.GetProperty("detail").GetString());
            // Gövdede hiçbir yazımla MFA token'ı bulunmaz (alan adı büyük/küçük harf duyarsız aranır).
            Assert.DoesNotContain("multiFactorToken", unknownEmail.GetRawText(), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("multiFactorToken", wrongPassword.GetRawText(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
