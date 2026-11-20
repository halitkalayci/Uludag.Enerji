using System.Net;
using ECommerce.IntegrationTests.Infrastructure;

namespace ECommerce.IntegrationTests.Catalog
{
    // Spec 0009 — kategori yönetimi (AC-1…AC-5, BR-12). Beklentiler spec'ten; her istek gerçek HTTP sözleşmesiyle.
    // Aynı koleksiyondaki testler sırayla koşar: liste sayımı başka bir testin eşzamanlı yazmasından etkilenmez.
    [Collection(IntegrationCollection.Name)]
    [Trait("Category", "Integration")]
    public class CategoryTests(ApiFixture fixture)
    {
        private readonly CatalogArrange arrange = new(fixture);

        public static TheoryData<string> InvalidNames => new()
        {
            "",
            "   ",
            CatalogArrange.UniqueNameOfLength(101),
            "  " + CatalogArrange.UniqueNameOfLength(101) + "  ",
        };

        // AC-1
        [Fact]
        public async Task Create_ValidName_IsListedAndReadableTrimmed()
        {
            using HttpClient client = await arrange.CreateClientWithClaimsAsync("Category.Create", "Category.Read");
            string name = CatalogArrange.NewCategoryName();

            HttpResponseMessage response = await CatalogArrange.PostCategoryAsync(client, $"  {name}  ");

            Assert.True(response.IsSuccessStatusCode, $"Oluşturma başarısız: {(int)response.StatusCode}");
            CategoryDto listed = Assert.Single(await CatalogArrange.GetCategoriesAsync(client), c => c.Name.Contains(name, StringComparison.Ordinal));
            Assert.Equal(name, listed.Name);

            HttpResponseMessage single = await CatalogArrange.GetCategoryAsync(client, listed.Id);
            Assert.Equal(HttpStatusCode.OK, single.StatusCode);
            Assert.Equal(listed, await AuthArrange.ReadJsonAsync<CategoryDto>(single));
        }

        // AC-2
        [Theory]
        [MemberData(nameof(InvalidNames))]
        public async Task Create_InvalidName_Returns400AndCreatesNothing(string name)
        {
            using HttpClient client = await arrange.CreateCatalogAdminClientAsync();
            int countBefore = (await CatalogArrange.GetCategoriesAsync(client)).Count;

            HttpResponseMessage response = await CatalogArrange.PostCategoryAsync(client, name);

            await ProblemAssert.IsProblemAsync(response, HttpStatusCode.BadRequest);
            Assert.Equal(countBefore, (await CatalogArrange.GetCategoriesAsync(client)).Count);
        }

        // AC-2 (sınır: tam 100 karakter kabul edilir)
        [Fact]
        public async Task Create_NameOf100Chars_IsAccepted()
        {
            using HttpClient client = await arrange.CreateCatalogAdminClientAsync();
            string name = CatalogArrange.UniqueNameOfLength(100);

            HttpResponseMessage response = await CatalogArrange.PostCategoryAsync(client, name);

            Assert.True(response.IsSuccessStatusCode, $"100 karakterlik ad reddedildi: {(int)response.StatusCode}");
            Assert.Single(await CatalogArrange.GetCategoriesAsync(client), c => c.Name == name);
        }

        // AC-2 (ad değiştirme)
        [Theory]
        [MemberData(nameof(InvalidNames))]
        public async Task Update_InvalidName_Returns400AndKeepsName(string name)
        {
            using HttpClient client = await arrange.CreateCatalogAdminClientAsync();
            CategoryDto category = await CatalogArrange.CreateCategoryAsync(client);

            HttpResponseMessage response = await CatalogArrange.PutCategoryAsync(client, category.Id, name);

            await ProblemAssert.IsProblemAsync(response, HttpStatusCode.BadRequest);
            HttpResponseMessage single = await CatalogArrange.GetCategoryAsync(client, category.Id);
            Assert.Equal(category, await AuthArrange.ReadJsonAsync<CategoryDto>(single));
        }

        // AC-3
        [Fact]
        public async Task Create_DuplicateNameAnyCase_Returns400()
        {
            using HttpClient client = await arrange.CreateCatalogAdminClientAsync();
            CategoryDto existing = await CatalogArrange.CreateCategoryAsync(client);

            foreach (string duplicate in new[] { existing.Name, existing.Name.ToUpperInvariant(), $"  {existing.Name.ToLowerInvariant()} " })
            {
                HttpResponseMessage response = await CatalogArrange.PostCategoryAsync(client, duplicate);
                await ProblemAssert.IsProblemAsync(response, HttpStatusCode.BadRequest);
            }

            Assert.Single(await CatalogArrange.GetCategoriesAsync(client), c => string.Equals(c.Name, existing.Name, StringComparison.OrdinalIgnoreCase));
        }

        // AC-3 (başka kategorinin adına değiştirme)
        [Fact]
        public async Task Update_ToOtherCategoryName_Returns400()
        {
            using HttpClient client = await arrange.CreateCatalogAdminClientAsync();
            CategoryDto first = await CatalogArrange.CreateCategoryAsync(client);
            CategoryDto second = await CatalogArrange.CreateCategoryAsync(client);

            HttpResponseMessage response = await CatalogArrange.PutCategoryAsync(client, second.Id, first.Name.ToUpperInvariant());

            await ProblemAssert.IsProblemAsync(response, HttpStatusCode.BadRequest);
            HttpResponseMessage single = await CatalogArrange.GetCategoryAsync(client, second.Id);
            Assert.Equal(second, await AuthArrange.ReadJsonAsync<CategoryDto>(single));
        }

        // AC-3 (kendi adının farklı harf düzeni çakışma sayılmaz)
        [Fact]
        public async Task Update_OwnNameDifferentCase_IsAccepted()
        {
            using HttpClient client = await arrange.CreateCatalogAdminClientAsync();
            CategoryDto category = await CatalogArrange.CreateCategoryAsync(client);
            string renamed = category.Name.ToUpperInvariant();

            HttpResponseMessage response = await CatalogArrange.PutCategoryAsync(client, category.Id, renamed);

            Assert.True(response.IsSuccessStatusCode, $"Kendi adının harf düzeni reddedildi: {(int)response.StatusCode}");
            HttpResponseMessage single = await CatalogArrange.GetCategoryAsync(client, category.Id);
            Assert.Equal(category with { Name = renamed }, await AuthArrange.ReadJsonAsync<CategoryDto>(single));
        }

        // AC-4
        [Fact]
        public async Task UnknownId_GetUpdateDelete_Return404()
        {
            using HttpClient client = await arrange.CreateCatalogAdminClientAsync();
            Guid unknownId = Guid.NewGuid();

            await ProblemAssert.IsProblemAsync(await CatalogArrange.GetCategoryAsync(client, unknownId), HttpStatusCode.NotFound);
            await ProblemAssert.IsProblemAsync(await CatalogArrange.PutCategoryAsync(client, unknownId, CatalogArrange.NewCategoryName()), HttpStatusCode.NotFound);
            await ProblemAssert.IsProblemAsync(await CatalogArrange.DeleteCategoryAsync(client, unknownId), HttpStatusCode.NotFound);
        }

        // AC-5
        [Fact]
        public async Task Delete_WithoutProducts_RemovesCategory()
        {
            using HttpClient client = await arrange.CreateCatalogAdminClientAsync();
            CategoryDto category = await CatalogArrange.CreateCategoryAsync(client);

            HttpResponseMessage response = await CatalogArrange.DeleteCategoryAsync(client, category.Id);

            Assert.True(response.IsSuccessStatusCode, $"Silme başarısız: {(int)response.StatusCode}");
            Assert.DoesNotContain(await CatalogArrange.GetCategoriesAsync(client), c => c.Id == category.Id);
            await ProblemAssert.IsProblemAsync(await CatalogArrange.GetCategoryAsync(client, category.Id), HttpStatusCode.NotFound);
        }
    }
}
