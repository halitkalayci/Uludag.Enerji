using System.Net;
using System.Net.Http.Json;
using ECommerce.Application.Services.Repositories;
using ECommerce.Domain.Entities;
using ECommerce.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.IntegrationTests.Auth
{
    // Spec 0002 — beklenmeyen hata sözleşmesi (AC-11): 500 + ProblemDetails, iç detay sızmaz.
    // Hata, mevcut bir kod hatasına dayanmadan, bir bağımlılığın hata fırlatan test ikiziyle değiştirilmesiyle üretilir.
    [Collection(IntegrationCollection.Name)]
    [Trait("Category", "Integration")]
    public class AuthErrorContractTests(ApiFixture fixture)
    {
        // Gövdede görünmemesi gereken iç detay.
        private const string InternalDetail = "integration-test-internal-detail-7f3a";

        [Fact]
        public async Task Register_UnexpectedFailure_ReturnsGenericProblem500()
        {
            await using WebApplicationFactory<Program> failingFactory = fixture.Factory.WithWebHostBuilder(builder =>
                builder.ConfigureTestServices(services => services.AddScoped<IUserRepository, ThrowingUserRepository>()));
            using HttpClient client = failingFactory.CreateClient();

            HttpResponseMessage response = await client.PostAsJsonAsync(AuthArrange.RegisterPath, new { email = AuthArrange.NewEmail(), password = AuthArrange.Password }, TestContext.Current.CancellationToken);

            await ProblemAssert.IsProblemAsync(response, HttpStatusCode.InternalServerError);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.DoesNotContain(InternalDetail, body, StringComparison.Ordinal);
            Assert.DoesNotContain(nameof(InvalidOperationException), body, StringComparison.Ordinal);
            Assert.DoesNotContain(nameof(ThrowingUserRepository), body, StringComparison.Ordinal);
            Assert.DoesNotContain(" at ", body, StringComparison.Ordinal);
        }

        private sealed class ThrowingUserRepository : IUserRepository
        {
            public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new InvalidOperationException(InternalDetail);

            public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new InvalidOperationException(InternalDetail);

            public Task AddAsync(User user, CancellationToken cancellationToken = default) => throw new InvalidOperationException(InternalDetail);
        }
    }
}
