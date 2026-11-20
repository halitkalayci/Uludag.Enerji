using System.Net;
using System.Net.Http.Json;
using System.Text;
using ECommerce.Domain.Entities;
using ECommerce.IntegrationTests.Infrastructure;
using ECommerce.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.IntegrationTests.Auth
{
    // Spec 0002 — kayıt (AC-1…AC-4, BR-8). Beklentiler spec'ten; kayıt isteği her zaman gerçek HTTP sözleşmesiyle.
    [Collection(IntegrationCollection.Name)]
    [Trait("Category", "Integration")]
    public class RegisterTests(ApiFixture fixture)
    {
        private readonly AuthArrange arrange = new(fixture);

        // AC-1
        [Fact]
        public async Task Register_NewEmail_CreatesUserWithoutPlainPassword()
        {
            using HttpClient client = arrange.CreateClient();
            string email = AuthArrange.NewEmail();

            HttpResponseMessage response = await client.PostAsJsonAsync(AuthArrange.RegisterPath, new { email, password = AuthArrange.Password }, TestContext.Current.CancellationToken);

            Assert.True(response.IsSuccessStatusCode, $"Kayıt başarısız: {(int)response.StatusCode}");

            List<User> users = await FindUsersAsync(email);
            User user = Assert.Single(users);
            Assert.NotEmpty(user.PasswordHash);
            Assert.NotEmpty(user.PasswordSalt);

            byte[] plainPassword = Encoding.UTF8.GetBytes(AuthArrange.Password);
            Assert.False(Contains(user.PasswordHash, plainPassword), "Şifre hash alanında düz metin olarak duruyor.");
            Assert.False(Contains(user.PasswordSalt, plainPassword), "Şifre salt alanında düz metin olarak duruyor.");
            Assert.DoesNotContain(AuthArrange.Password, user.Email, StringComparison.Ordinal);
        }

        // AC-2
        [Fact]
        public async Task Register_ExistingEmail_ReturnsProblem400AndNoSecondUser()
        {
            string email = AuthArrange.NewEmail();
            await arrange.CreateUserAsync(email);
            using HttpClient client = arrange.CreateClient();

            HttpResponseMessage response = await client.PostAsJsonAsync(AuthArrange.RegisterPath, new { email, password = "Another-Password-2!" }, TestContext.Current.CancellationToken);

            await ProblemAssert.IsProblemAsync(response, HttpStatusCode.BadRequest);
            Assert.Single(await FindUsersAsync(email));
        }

        // AC-2 (büyük/küçük harf duyarsız benzersizlik)
        [Fact]
        public async Task Register_ExistingEmailDifferentCase_ReturnsProblem400AndNoSecondUser()
        {
            string email = AuthArrange.NewEmail();
            await arrange.CreateUserAsync(email.ToUpperInvariant());
            using HttpClient client = arrange.CreateClient();

            HttpResponseMessage response = await client.PostAsJsonAsync(AuthArrange.RegisterPath, new { email, password = AuthArrange.Password }, TestContext.Current.CancellationToken);

            await ProblemAssert.IsProblemAsync(response, HttpStatusCode.BadRequest);
            Assert.Single(await FindUsersAsync(email));
        }

        // AC-3
        [Fact]
        public async Task Register_TenConcurrentSameEmail_ExactlyOneSucceeds()
        {
            // Yarış durumu her denemede ortaya çıkmayabilir: kriter (10 eşzamanlı istek → tam olarak biri başarılı)
            // birkaç turda, her turda yeni bir e-postayla doğrulanır. Doğru bir sistem her turu geçer.
            const int rounds = 5;
            const int requestCount = 10;
            List<string> violations = [];

            for (int round = 1; round <= rounds; round++)
            {
                string email = AuthArrange.NewEmail();
                HttpClient[] clients = Enumerable.Range(0, requestCount).Select(_ => arrange.CreateClient()).ToArray();
                // İstekler ayrı thread'lerde hazırlanır ve bariyerde aynı anda bırakılır: sıcak bir uygulamada
                // istekler sırayla işlenip yarış durumu gizlenmesin.
                using Barrier startLine = new(requestCount);

                HttpResponseMessage[] responses = await Task.WhenAll(clients.Select(client => Task.Run(() =>
                {
                    startLine.SignalAndWait(TestContext.Current.CancellationToken);
                    return client.PostAsJsonAsync(AuthArrange.RegisterPath, new { email, password = AuthArrange.Password }, TestContext.Current.CancellationToken);
                })));
                foreach (HttpClient client in clients)
                    client.Dispose();

                string observed = string.Join(", ", responses.Select(r => (int)r.StatusCode).OrderBy(s => s));
                int successCount = responses.Count(r => r.IsSuccessStatusCode);
                bool othersAre400 = responses.Where(r => !r.IsSuccessStatusCode).All(r => r.StatusCode == HttpStatusCode.BadRequest);
                int userCount = (await FindUsersAsync(email)).Count;

                if (successCount != 1 || !othersAre400 || userCount != 1)
                    violations.Add($"tur {round}: başarılı={successCount}, kullanıcı sayısı={userCount}, durum kodları=[{observed}]");
            }

            Assert.True(violations.Count == 0,
                "Tam olarak bir başarılı kayıt ve tek kullanıcı bekleniyordu (diğerleri 400):" + Environment.NewLine + string.Join(Environment.NewLine, violations));
        }

        // AC-4 (eksik / boş alan)
        [Theory]
        [InlineData("email-missing")]
        [InlineData("password-missing")]
        [InlineData("email-empty")]
        [InlineData("password-empty")]
        public async Task Register_MissingOrEmptyField_ReturnsProblem400(string invalidCase)
        {
            string email = AuthArrange.NewEmail();
            object body = invalidCase switch
            {
                "email-missing" => new { password = AuthArrange.Password },
                "password-missing" => new { email },
                "email-empty" => new { email = "", password = AuthArrange.Password },
                "password-empty" => new { email, password = "" },
                _ => throw new ArgumentOutOfRangeException(nameof(invalidCase))
            };
            using HttpClient client = arrange.CreateClient();
            // E-posta hiç gönderilmeyen vakada adresle arama anlamsızdır: toplam kullanıcı sayısı karşılaştırılır
            // (entegrasyon testleri tek collection'da sırayla koşar, sayım deterministiktir).
            int usersBefore = await CountUsersAsync();

            HttpResponseMessage response = await client.PostAsJsonAsync(AuthArrange.RegisterPath, body, TestContext.Current.CancellationToken);

            await ProblemAssert.IsProblemAsync(response, HttpStatusCode.BadRequest);
            Assert.Equal(usersBefore, await CountUsersAsync());
            // Boş e-posta vakasında oluşabilecek kullanıcı boş adreslidir; diğerlerinde üretilen adresle aranır.
            Assert.Empty(await FindUsersAsync(invalidCase == "email-empty" ? "" : email));
        }

        // AC-4 (açıkça geçersiz e-posta biçimi)
        [Theory]
        [InlineData("abc")]
        [InlineData("a@")]
        [InlineData("@x.com")]
        [InlineData("a b@x.com")]
        public async Task Register_InvalidEmailFormat_ReturnsProblem400(string invalidEmail)
        {
            using HttpClient client = arrange.CreateClient();

            HttpResponseMessage response = await client.PostAsJsonAsync(AuthArrange.RegisterPath, new { email = invalidEmail, password = AuthArrange.Password }, TestContext.Current.CancellationToken);

            await ProblemAssert.IsProblemAsync(response, HttpStatusCode.BadRequest);
            Assert.Empty(await FindUsersAsync(invalidEmail));
        }

        private async Task<int> CountUsersAsync()
        {
            await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
            UludagDbContext dbContext = scope.ServiceProvider.GetRequiredService<UludagDbContext>();
            return await dbContext.Users.CountAsync(TestContext.Current.CancellationToken);
        }

        // Büyük/küçük harf duyarsız arama: aynı adresin farklı yazımları da sayılır.
        private async Task<List<User>> FindUsersAsync(string email)
        {
            await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
            UludagDbContext dbContext = scope.ServiceProvider.GetRequiredService<UludagDbContext>();
            string normalized = email.ToLower();
            return await dbContext.Users.AsNoTracking()
                .Where(u => u.Email.ToLower() == normalized)
                .ToListAsync(TestContext.Current.CancellationToken);
        }

        private static bool Contains(byte[] haystack, byte[] needle) =>
            needle.Length > 0 && haystack.AsSpan().IndexOf(needle) >= 0;
    }
}
