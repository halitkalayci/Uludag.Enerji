using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.IntegrationTests.Infrastructure
{
    // Katalog (ürün + kategori, spec 0009) testlerinin HAZIRLIK adımları. Kullanıcı ve yetkileri uygulamanın kendi
    // DbContext'iyle yazılır; token gerçek MFA akışıyla alınır (AuthArrange). Token oturumu yetkileri girişte okur:
    // yetki önce atanır, sonra giriş yapılır. Testin konusu olan istek her zaman testin kendisinde, HTTP ile yapılır.
    public sealed class CatalogArrange(ApiFixture fixture)
    {
        public const string CategoriesPath = "/api/categories";
        public const string ProductsPath = "/api/products";

        // Spec 0009 öncesinden kalan seed ürün (ProductConfiguration, add_product_seed migration'ı).
        public static readonly Guid SeedProductId = Guid.Parse("2dcb27b9-06cf-4053-a423-c2e94ed048a6");

        private readonly AuthArrange auth = new(fixture);

        public HttpClient CreateAnonymousClient() => auth.CreateClient();

        // Verilen operation claim'lere sahip yeni bir kullanıcının access token'ıyla yetkilendirilmiş istemci.
        public async Task<HttpClient> CreateClientWithClaimsAsync(params string[] claimNames)
        {
            Guid userId = await auth.CreateUserAsync(AuthArrange.NewEmail());
            await AssignClaimsAsync(userId, claimNames);

            HttpClient client = auth.CreateClient();
            (_, TokenResult tokens) = await auth.EnableMfaAsync(client, userId);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
            return client;
        }

        // Hem kategori hem ürün işlemlerinin tamamına yetkili istemci (konusu yetki olmayan testler için).
        public Task<HttpClient> CreateCatalogAdminClientAsync() => CreateClientWithClaimsAsync("Category.Admin", "Product.Admin");

        private async Task AssignClaimsAsync(Guid userId, string[] claimNames)
        {
            await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
            UludagDbContext dbContext = scope.ServiceProvider.GetRequiredService<UludagDbContext>();

            List<OperationClaim> claims = await dbContext.OperationClaims
                .Where(c => claimNames.Contains(c.Name))
                .ToListAsync(TestContext.Current.CancellationToken);
            Assert.Equal(claimNames.Length, claims.Count);

            dbContext.UserOperationClaims.AddRange(claims.Select(c => new UserOperationClaim
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                OperationClaimId = c.Id
            }));
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        public static string NewCategoryName() => $"Kategori {Guid.NewGuid():N}";

        public static string NewProductName() => $"Ürün {Guid.NewGuid():N}";

        // Tam olarak <length> karakterlik, koşu içinde benzersiz bir ad.
        public static string UniqueNameOfLength(int length)
        {
            string unique = Guid.NewGuid().ToString("N");
            return unique + new string('k', length - unique.Length);
        }

        // --- Kategori ---

        public static Task<HttpResponseMessage> PostCategoryAsync(HttpClient client, string? name) =>
            client.PostAsJsonAsync(CategoriesPath, new { name }, TestContext.Current.CancellationToken);

        public static Task<HttpResponseMessage> PutCategoryAsync(HttpClient client, Guid id, string? name) =>
            client.PutAsJsonAsync($"{CategoriesPath}/{id}", new { name }, TestContext.Current.CancellationToken);

        public static Task<HttpResponseMessage> GetCategoryAsync(HttpClient client, Guid id) =>
            client.GetAsync($"{CategoriesPath}/{id}", TestContext.Current.CancellationToken);

        public static Task<HttpResponseMessage> DeleteCategoryAsync(HttpClient client, Guid id) =>
            client.DeleteAsync($"{CategoriesPath}/{id}", TestContext.Current.CancellationToken);

        public static async Task<List<CategoryDto>> GetCategoriesAsync(HttpClient client)
        {
            HttpResponseMessage response = await client.GetAsync(CategoriesPath, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await AuthArrange.ReadJsonAsync<List<CategoryDto>>(response);
        }

        // Kategori oluşturur ve listedeki kaydını döner (oluşturma yanıtı gövde taşımaz).
        public static async Task<CategoryDto> CreateCategoryAsync(HttpClient client, string? name = null)
        {
            name ??= NewCategoryName();
            HttpResponseMessage response = await PostCategoryAsync(client, name);
            Assert.True(response.IsSuccessStatusCode, $"Kategori oluşturulamadı: {(int)response.StatusCode}");
            return Assert.Single(await GetCategoriesAsync(client), c => c.Name == name.Trim());
        }

        // --- Ürün ---

        public static Task<HttpResponseMessage> PostProductAsync(HttpClient client, string name, Guid? categoryId) =>
            client.PostAsJsonAsync(ProductsPath, new { name, amount = 10m, currency = "TRY", stock = 5, categoryId }, TestContext.Current.CancellationToken);

        public static Task<HttpResponseMessage> PutProductAsync(HttpClient client, ProductDto product, Guid? categoryId) =>
            client.PutAsJsonAsync($"{ProductsPath}/{product.Id}", new { name = product.Name, amount = product.Amount, currency = product.Currency, stock = product.Stock, categoryId }, TestContext.Current.CancellationToken);

        // categoryId alanı hiç gönderilmeden yapılan güncelleme (spec 0009, AC-8).
        public static Task<HttpResponseMessage> PutProductWithoutCategoryFieldAsync(HttpClient client, ProductDto product) =>
            client.PutAsJsonAsync($"{ProductsPath}/{product.Id}", new { name = product.Name, amount = product.Amount, currency = product.Currency, stock = product.Stock }, TestContext.Current.CancellationToken);

        public static async Task<List<ProductDto>> GetProductsAsync(HttpClient client)
        {
            HttpResponseMessage response = await client.GetAsync(ProductsPath, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await AuthArrange.ReadJsonAsync<List<ProductDto>>(response);
        }

        public static async Task<ProductDto> GetProductAsync(HttpClient client, Guid id)
        {
            HttpResponseMessage response = await client.GetAsync($"{ProductsPath}/{id}", TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await AuthArrange.ReadJsonAsync<ProductDto>(response);
        }

        // Ürün oluşturur ve listedeki kaydını döner.
        public static async Task<ProductDto> CreateProductAsync(HttpClient client, Guid? categoryId)
        {
            string name = NewProductName();
            HttpResponseMessage response = await PostProductAsync(client, name, categoryId);
            Assert.True(response.IsSuccessStatusCode, $"Ürün oluşturulamadı: {(int)response.StatusCode}");
            return Assert.Single(await GetProductsAsync(client), p => p.Name == name);
        }
    }

    // Sözleşmenin istemcinin göreceği şekli (uygulamanın response sınıfları kullanılmaz).
    public sealed record CategoryDto(Guid Id, string Name);

    public sealed record ProductDto(Guid Id, string? Name, decimal Amount, string Currency, int Stock, Guid? CategoryId, string? CategoryName);
}
