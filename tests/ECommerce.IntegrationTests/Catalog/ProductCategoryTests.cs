using System.Net;
using ECommerce.IntegrationTests.Infrastructure;

namespace ECommerce.IntegrationTests.Catalog
{
    // Spec 0009 — ürünün kategorisi (AC-6…AC-9, AC-11; BR-13, BR-14). Beklentiler spec'ten; her istek HTTP ile.
    [Collection(IntegrationCollection.Name)]
    [Trait("Category", "Integration")]
    public class ProductCategoryTests(ApiFixture fixture)
    {
        private readonly CatalogArrange arrange = new(fixture);

        public static TheoryData<Guid> UnknownCategoryIds => new() { Guid.NewGuid(), Guid.Empty };

        // AC-6
        [Fact]
        public async Task DeleteCategory_WithProduct_Returns400ThenSucceedsAfterUnassign()
        {
            using HttpClient client = await arrange.CreateCatalogAdminClientAsync();
            CategoryDto category = await CatalogArrange.CreateCategoryAsync(client);
            ProductDto product = await CatalogArrange.CreateProductAsync(client, category.Id);

            await ProblemAssert.IsProblemAsync(await CatalogArrange.DeleteCategoryAsync(client, category.Id), HttpStatusCode.BadRequest);
            HttpResponseMessage stillThere = await CatalogArrange.GetCategoryAsync(client, category.Id);
            Assert.Equal(category, await AuthArrange.ReadJsonAsync<CategoryDto>(stillThere));

            HttpResponseMessage unassign = await CatalogArrange.PutProductAsync(client, product, categoryId: null);
            Assert.True(unassign.IsSuccessStatusCode, $"Kategori kaldırılamadı: {(int)unassign.StatusCode}");

            HttpResponseMessage delete = await CatalogArrange.DeleteCategoryAsync(client, category.Id);
            Assert.True(delete.IsSuccessStatusCode, $"Ürünsüz kalan kategori silinemedi: {(int)delete.StatusCode}");
            await ProblemAssert.IsProblemAsync(await CatalogArrange.GetCategoryAsync(client, category.Id), HttpStatusCode.NotFound);
        }

        // AC-7
        [Fact]
        public async Task Create_WithCategory_ResponseShowsCategory()
        {
            using HttpClient client = await arrange.CreateCatalogAdminClientAsync();
            CategoryDto category = await CatalogArrange.CreateCategoryAsync(client);

            ProductDto listed = await CatalogArrange.CreateProductAsync(client, category.Id);

            Assert.Equal(category.Id, listed.CategoryId);
            Assert.Equal(category.Name, listed.CategoryName);
            ProductDto single = await CatalogArrange.GetProductAsync(client, listed.Id);
            Assert.Equal(category.Id, single.CategoryId);
            Assert.Equal(category.Name, single.CategoryName);
        }

        // AC-7 (kategorisiz)
        [Fact]
        public async Task Create_WithoutCategory_CategoryIsNull()
        {
            using HttpClient client = await arrange.CreateCatalogAdminClientAsync();

            ProductDto listed = await CatalogArrange.CreateProductAsync(client, categoryId: null);

            Assert.Null(listed.CategoryId);
            Assert.Null(listed.CategoryName);
            ProductDto single = await CatalogArrange.GetProductAsync(client, listed.Id);
            Assert.Null(single.CategoryId);
            Assert.Null(single.CategoryName);
        }

        // AC-7 (kategori yeniden adlandırılınca ürün yanıtı yeni adı gösterir)
        [Fact]
        public async Task RenameCategory_ProductShowsNewName()
        {
            using HttpClient client = await arrange.CreateCatalogAdminClientAsync();
            CategoryDto category = await CatalogArrange.CreateCategoryAsync(client);
            ProductDto product = await CatalogArrange.CreateProductAsync(client, category.Id);
            string newName = CatalogArrange.NewCategoryName();

            HttpResponseMessage rename = await CatalogArrange.PutCategoryAsync(client, category.Id, newName);
            Assert.True(rename.IsSuccessStatusCode, $"Yeniden adlandırma başarısız: {(int)rename.StatusCode}");

            Assert.Equal(newName, (await CatalogArrange.GetProductAsync(client, product.Id)).CategoryName);
            Assert.Equal(newName, Assert.Single(await CatalogArrange.GetProductsAsync(client), p => p.Id == product.Id).CategoryName);
        }

        // AC-8
        [Fact]
        public async Task Update_ChangeAndRemoveCategory_ReflectedInReads()
        {
            using HttpClient client = await arrange.CreateCatalogAdminClientAsync();
            CategoryDto first = await CatalogArrange.CreateCategoryAsync(client);
            CategoryDto second = await CatalogArrange.CreateCategoryAsync(client);
            ProductDto product = await CatalogArrange.CreateProductAsync(client, first.Id);

            // Başka kategoriye taşı.
            Assert.True((await CatalogArrange.PutProductAsync(client, product, second.Id)).IsSuccessStatusCode);
            await AssertProductCategoryAsync(client, product.Id, second);

            // null ile kaldır.
            Assert.True((await CatalogArrange.PutProductAsync(client, product, categoryId: null)).IsSuccessStatusCode);
            await AssertProductCategoryAsync(client, product.Id, expected: null);

            // Yeniden ata, sonra alanı hiç göndermeden güncelle -> kategorisiz (tam değiştirme, spec R-3).
            Assert.True((await CatalogArrange.PutProductAsync(client, product, first.Id)).IsSuccessStatusCode);
            await AssertProductCategoryAsync(client, product.Id, first);
            Assert.True((await CatalogArrange.PutProductWithoutCategoryFieldAsync(client, product)).IsSuccessStatusCode);
            await AssertProductCategoryAsync(client, product.Id, expected: null);
        }

        // AC-9 (oluşturma)
        [Theory]
        [MemberData(nameof(UnknownCategoryIds))]
        public async Task Create_UnknownCategory_Returns404AndCreatesNothing(Guid unknownCategoryId)
        {
            using HttpClient client = await arrange.CreateCatalogAdminClientAsync();
            string name = CatalogArrange.NewProductName();

            HttpResponseMessage response = await CatalogArrange.PostProductAsync(client, name, unknownCategoryId);

            await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound);
            Assert.DoesNotContain(await CatalogArrange.GetProductsAsync(client), p => p.Name == name);
        }

        // AC-9 (güncelleme)
        [Theory]
        [MemberData(nameof(UnknownCategoryIds))]
        public async Task Update_UnknownCategory_Returns404AndKeepsCategory(Guid unknownCategoryId)
        {
            using HttpClient client = await arrange.CreateCatalogAdminClientAsync();
            CategoryDto category = await CatalogArrange.CreateCategoryAsync(client);
            ProductDto product = await CatalogArrange.CreateProductAsync(client, category.Id);

            HttpResponseMessage response = await CatalogArrange.PutProductAsync(client, product, unknownCategoryId);

            await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound);
            await AssertProductCategoryAsync(client, product.Id, category);
        }

        // AC-11
        [Fact]
        public async Task SeedProduct_IsUnchangedAndUncategorized()
        {
            using HttpClient client = await arrange.CreateCatalogAdminClientAsync();

            ProductDto seed = await CatalogArrange.GetProductAsync(client, CatalogArrange.SeedProductId);

            Assert.Equal(new ProductDto(CatalogArrange.SeedProductId, "Deneme ürün", 500m, "TRY", 100, null, null), seed);
            Assert.Equal(seed, Assert.Single(await CatalogArrange.GetProductsAsync(client), p => p.Id == CatalogArrange.SeedProductId));
        }

        private static async Task AssertProductCategoryAsync(HttpClient client, Guid productId, CategoryDto? expected)
        {
            ProductDto single = await CatalogArrange.GetProductAsync(client, productId);
            Assert.Equal(expected?.Id, single.CategoryId);
            Assert.Equal(expected?.Name, single.CategoryName);

            ProductDto listed = Assert.Single(await CatalogArrange.GetProductsAsync(client), p => p.Id == productId);
            Assert.Equal(expected?.Id, listed.CategoryId);
            Assert.Equal(expected?.Name, listed.CategoryName);
        }
    }
}
