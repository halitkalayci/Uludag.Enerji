using System.Net;
using ECommerce.IntegrationTests.Infrastructure;

namespace ECommerce.IntegrationTests.Catalog
{
    // Spec 0009 — kategori işlemlerinin yetkisi (AC-10, R-7, BR-11). Kanıt kapsamı spec'te: her işlem için yetkisiz → 403,
    // kabul edilen yetkilerden bir örnek ve global Admin; tam kombinasyon matrisi gerekmez.
    [Collection(IntegrationCollection.Name)]
    [Trait("Category", "Integration")]
    public class CategoryAuthorizationTests(ApiFixture fixture)
    {
        private readonly CatalogArrange arrange = new(fixture);

        public static TheoryData<string> Operations => new() { "Create", "GetAll", "GetById", "Update", "Delete" };

        // İşlem -> spec R-7'de kabul edilen yetkilerden bir örnek (Write, Admin ve işlemin kendi yetkisi dağıtılarak).
        public static TheoryData<string, string> AcceptedClaims => new()
        {
            { "Create", "Category.Create" },
            { "Create", "Category.Write" },
            { "GetAll", "Category.Read" },
            { "GetById", "Category.Admin" },
            { "Update", "Category.Update" },
            { "Delete", "Category.Delete" },
            { "Delete", "Category.Write" },
        };

        // AC-10: token'sız istek -> 401
        [Theory]
        [MemberData(nameof(Operations))]
        public async Task NoToken_Returns401(string operation)
        {
            using HttpClient admin = await arrange.CreateCatalogAdminClientAsync();
            CategoryDto existing = await CatalogArrange.CreateCategoryAsync(admin);
            using HttpClient anonymous = arrange.CreateAnonymousClient();

            HttpResponseMessage response = await SendAsync(anonymous, operation, existing.Id);

            await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Unauthorized);
            await AssertCategoryUntouchedAsync(admin, existing);
        }

        // AC-10: Category.* yetkisi olmayan kullanıcı (başka feature'ın tam yetkisi olsa da) -> 403
        [Theory]
        [MemberData(nameof(Operations))]
        public async Task WithoutCategoryClaim_Returns403(string operation)
        {
            using HttpClient admin = await arrange.CreateCatalogAdminClientAsync();
            CategoryDto existing = await CatalogArrange.CreateCategoryAsync(admin);
            using HttpClient client = await arrange.CreateClientWithClaimsAsync("Product.Admin", "Order.Admin");

            HttpResponseMessage response = await SendAsync(client, operation, existing.Id);

            await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Forbidden);
            await AssertCategoryUntouchedAsync(admin, existing);
        }

        // Bir işlemin Category yetkisi başka bir işleme sızmaz (review round 1, L-1): yetki "bir Category.* claim'i" değil,
        // R-7'de o işlem için sayılanlardan biridir.
        public static TheoryData<string, string> OtherOperationClaims => new()
        {
            { "Create", "Category.Read" },
            { "Update", "Category.Create" },
            { "Delete", "Category.Update" },
            { "Delete", "Category.Read" },
            { "GetAll", "Category.Write" },
            { "GetById", "Category.Delete" },
        };

        // AC-10: ilgili işlemin yetkisi olmayan (yalnızca başka bir Category işleminin yetkisi olan) kullanıcı -> 403
        [Theory]
        [MemberData(nameof(OtherOperationClaims))]
        public async Task OtherOperationClaim_Returns403(string operation, string claim)
        {
            using HttpClient admin = await arrange.CreateCatalogAdminClientAsync();
            CategoryDto existing = await CatalogArrange.CreateCategoryAsync(admin);
            using HttpClient client = await arrange.CreateClientWithClaimsAsync(claim);

            HttpResponseMessage response = await SendAsync(client, operation, existing.Id);

            await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Forbidden);
            await AssertCategoryUntouchedAsync(admin, existing);
        }

        // AC-10: R-7'deki yetkilerden herhangi biri yeterli
        [Theory]
        [MemberData(nameof(AcceptedClaims))]
        public async Task AcceptedClaim_IsAllowed(string operation, string claim)
        {
            using HttpClient admin = await arrange.CreateCatalogAdminClientAsync();
            CategoryDto existing = await CatalogArrange.CreateCategoryAsync(admin);
            using HttpClient client = await arrange.CreateClientWithClaimsAsync(claim);

            HttpResponseMessage response = await SendAsync(client, operation, existing.Id);

            Assert.True(response.IsSuccessStatusCode, $"{claim} ile {operation} reddedildi: {(int)response.StatusCode}");
        }

        // AC-10: global Admin her işlemden geçer
        [Theory]
        [MemberData(nameof(Operations))]
        public async Task GlobalAdmin_IsAllowed(string operation)
        {
            using HttpClient admin = await arrange.CreateCatalogAdminClientAsync();
            CategoryDto existing = await CatalogArrange.CreateCategoryAsync(admin);
            using HttpClient client = await arrange.CreateClientWithClaimsAsync("Admin");

            HttpResponseMessage response = await SendAsync(client, operation, existing.Id);

            Assert.True(response.IsSuccessStatusCode, $"Global Admin ile {operation} reddedildi: {(int)response.StatusCode}");
        }

        private static Task<HttpResponseMessage> SendAsync(HttpClient client, string operation, Guid existingId) => operation switch
        {
            "Create" => CatalogArrange.PostCategoryAsync(client, CatalogArrange.NewCategoryName()),
            "GetAll" => client.GetAsync(CatalogArrange.CategoriesPath, TestContext.Current.CancellationToken),
            "GetById" => CatalogArrange.GetCategoryAsync(client, existingId),
            "Update" => CatalogArrange.PutCategoryAsync(client, existingId, CatalogArrange.NewCategoryName()),
            "Delete" => CatalogArrange.DeleteCategoryAsync(client, existingId),
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null),
        };

        // Reddedilen istek hiçbir şeyi değiştirmemiş olmalı.
        private static async Task AssertCategoryUntouchedAsync(HttpClient admin, CategoryDto existing)
        {
            HttpResponseMessage single = await CatalogArrange.GetCategoryAsync(admin, existing.Id);
            Assert.Equal(HttpStatusCode.OK, single.StatusCode);
            Assert.Equal(existing, await AuthArrange.ReadJsonAsync<CategoryDto>(single));
        }
    }
}
