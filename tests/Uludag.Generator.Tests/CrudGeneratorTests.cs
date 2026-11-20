using Uludag.Generator.Tests.Infrastructure;
using static Uludag.Generator.Tests.Infrastructure.Generated;

namespace Uludag.Generator.Tests
{
    // AC-2 (dosyalar), AC-6 (özellik eşleme), AC-7 (çakışma/force), AC-8 (hatalı girdi, dry-run).
    public class CrudGeneratorTests
    {
        public static readonly string[] CategoryFiles =
        [
            "ECommerce/src/ECommerce.Application/Features/Category/Commands/Create/CreateCategoryCommand.cs",
            "ECommerce/src/ECommerce.Application/Features/Category/Commands/Update/UpdateCategoryCommand.cs",
            "ECommerce/src/ECommerce.Application/Features/Category/Commands/Delete/DeleteCategoryCommand.cs",
            "ECommerce/src/ECommerce.Application/Features/Category/Queries/GetAll/GetAllCategoryQuery.cs",
            "ECommerce/src/ECommerce.Application/Features/Category/Queries/GetById/GetByIdCategoryQuery.cs",
            "ECommerce/src/ECommerce.Application/Features/Category/Queries/CategoryResponse.cs",
            "ECommerce/src/ECommerce.Application/Features/Category/Rules/CategoryBusinessRules.cs",
            "ECommerce/src/ECommerce.Application/Features/Category/Mappers/CategoryMapper.cs",
            "ECommerce/src/ECommerce.Application/Features/Category/Constants/CategoryOperationClaims.cs",
            "ECommerce/src/ECommerce.Application/Features/Category/Constants/CategoryConstants.cs",
            "ECommerce/src/ECommerce.Application/Features/Category/Locales/CategoryLocale.tr.yaml",
            "ECommerce/src/ECommerce.Application/Features/Category/Locales/CategoryLocale.en.yaml",
            "ECommerce/src/ECommerce.Application/Services/Repositories/ICategoryRepository.cs",
            "ECommerce/src/ECommerce.Persistence/Repositories/CategoryRepository.cs",
            "ECommerce/src/ECommerce.Persistence/Configurations/CategoryConfiguration.cs",
            "ECommerce/src/ECommerce.WebAPI/Controllers/CategoriesController.cs",
        ];

        private static TestRepo CategoryRepo()
        {
            var repo = TestRepo.Create();
            repo.AddEntity("Category", Fixtures.Category);
            return repo;
        }

        [Fact]
        public void Crud_FixtureEntity_Writes16FilesAnd3Registrations()
        {
            using var repo = CategoryRepo();
            var before = repo.Snapshot();

            RunResult result = Run(repo, "crud", "Category");

            Assert.Equal(ExitCodes.Success, result.ExitCode);
            var after = repo.Snapshot();
            var added = after.Keys.Except(before.Keys).Order().ToList();
            Assert.Equal(CategoryFiles.Order(), added);
            foreach (string file in CategoryFiles)
                Assert.Contains($"+ {file}", result.Output);
            Assert.Contains("ZORUNLU — migration", result.Output);
            Assert.Contains("dotnet ef migrations add add_category", result.Output);
        }

        [Fact]
        public void Map_Id_NotInCreate_RouteFieldInUpdate()
        {
            using var repo = CategoryRepo();
            Run(repo, "crud", "Category");

            var create = Properties(FeatureFile(repo, "Category", "Commands/Create/CreateCategoryCommand.cs"), "CreateCategoryCommand");
            var update = Properties(FeatureFile(repo, "Category", "Commands/Update/UpdateCategoryCommand.cs"), "UpdateCategoryCommand");
            var response = Properties(FeatureFile(repo, "Category", "Queries/CategoryResponse.cs"), "CategoryResponse");

            Assert.DoesNotContain("Id", create.Keys);
            Assert.Equal("Guid", update["Id"]);
            Assert.Equal("Guid", response["Id"]);
        }

        [Fact]
        public void Map_SingleMoney_FlattenedToAmountAndCurrency()
        {
            using var repo = CategoryRepo();
            Run(repo, "crud", "Category");

            var create = Properties(FeatureFile(repo, "Category", "Commands/Create/CreateCategoryCommand.cs"), "CreateCategoryCommand");
            string mapper = File.ReadAllText(FeatureFile(repo, "Category", "Mappers/CategoryMapper.cs"));

            Assert.Equal("decimal", create["Amount"]);
            Assert.Equal("string", create["Currency"]);
            Assert.DoesNotContain("MinimumPrice", create.Keys);
            Assert.Contains("MinimumPrice = new Domain.ValueObjects.Money(command.Amount, command.Currency)", mapper);
            Assert.Contains("Amount = category.MinimumPrice.Amount", mapper);
        }

        [Fact]
        public void Map_TwoMoney_PrefixedFields()
        {
            using var repo = TestRepo.Create();
            repo.AddEntity("Coupon", Fixtures.Coupon);

            Assert.Equal(ExitCodes.Success, Run(repo, "crud", "Coupon").ExitCode);

            var create = Properties(FeatureFile(repo, "Coupon", "Commands/Create/CreateCouponCommand.cs"), "CreateCouponCommand");
            Assert.Equal(["Code", "DiscountAmount", "DiscountCurrency", "MinimumOrderAmount", "MinimumOrderCurrency"], create.Keys);
            string mapper = File.ReadAllText(FeatureFile(repo, "Coupon", "Mappers/CouponMapper.cs"));
            Assert.Contains("Discount = new Domain.ValueObjects.Money(command.DiscountAmount, command.DiscountCurrency)", mapper);
            Assert.Contains("MinimumOrder = new Domain.ValueObjects.Money(command.MinimumOrderAmount, command.MinimumOrderCurrency)", mapper);
        }

        [Fact]
        public void Map_MoneyNameCollidesWithProperty_PrefixedFields()
        {
            // Gerçek OrderItem: int Amount + Money TotalPrice → düz "Amount" çakışırdı.
            using var repo = TestRepo.Create();

            Assert.Equal(ExitCodes.Success, Run(repo, "crud", "OrderItem").ExitCode);

            var create = Properties(FeatureFile(repo, "OrderItem", "Commands/Create/CreateOrderItemCommand.cs"), "CreateOrderItemCommand");
            Assert.Equal("int", create["Amount"]);
            Assert.Equal("decimal", create["TotalPriceAmount"]);
            Assert.Equal("string", create["TotalPriceCurrency"]);
        }

        [Fact]
        public void Map_NavigationAndCollection_SkippedAndReported()
        {
            using var repo = CategoryRepo();

            RunResult result = Run(repo, "crud", "Category");

            var response = Properties(FeatureFile(repo, "Category", "Queries/CategoryResponse.cs"), "CategoryResponse");
            Assert.DoesNotContain("User", response.Keys);
            Assert.DoesNotContain("Products", response.Keys);
            Assert.DoesNotContain("Image", response.Keys);
            Assert.Contains("- User (User) — navigation özelliği", result.Output);
            Assert.Contains("- Products (ICollection<Product>) — koleksiyon", result.Output);
            Assert.Contains("- Image (byte[]?) — tanınmayan tip", result.Output);
        }

        [Fact]
        public void Map_ForeignKeyNullableAndEnum_Kept()
        {
            using var repo = CategoryRepo();
            Run(repo, "crud", "Category");

            var create = Properties(FeatureFile(repo, "Category", "Commands/Create/CreateCategoryCommand.cs"), "CreateCategoryCommand");

            Assert.Equal("Guid?", create["ParentCategoryId"]);
            Assert.Equal("string?", create["Description"]);
            Assert.Equal("DateTime?", create["PublishedAt"]);
            Assert.Equal("CategoryStatus", create["Status"]);
            Assert.Equal("string", create["Name"]);
        }

        [Fact]
        public void Map_UserId_NotInCommands_InResponse_Warned()
        {
            using var repo = CategoryRepo();

            RunResult result = Run(repo, "crud", "Category");

            var create = Properties(FeatureFile(repo, "Category", "Commands/Create/CreateCategoryCommand.cs"), "CreateCategoryCommand");
            var update = Properties(FeatureFile(repo, "Category", "Commands/Update/UpdateCategoryCommand.cs"), "UpdateCategoryCommand");
            var response = Properties(FeatureFile(repo, "Category", "Queries/CategoryResponse.cs"), "CategoryResponse");
            Assert.DoesNotContain("UserId", create.Keys);
            Assert.DoesNotContain("UserId", update.Keys);
            Assert.Equal("Guid", response["UserId"]);
            Assert.Contains("! UserId — kimliğe bağlı alan", result.Output);
        }

        [Fact]
        public void Crud_PluralOverride_UsedForControllerAndDbSet()
        {
            using var repo = CategoryRepo();

            Run(repo, "crud", "Category", "--plural", "CategoryList");

            Assert.True(File.Exists(Path.Combine(repo.Layout.WebApiDir, "Controllers", "CategoryListController.cs")));
            Assert.Contains("uludagDbContext.CategoryList", File.ReadAllText(Path.Combine(repo.Layout.PersistenceDir, "Repositories", "CategoryRepository.cs")));
        }

        [Theory]
        [InlineData("Missing", "adlı entity")]
        [InlineData("RefreshTokenRevocationReason", "bir enum")]
        [InlineData("Money", "bir value object")]
        [InlineData("Tag", "'Guid Id' özelliği yok")]
        public void Crud_UnknownEnumOrNoGuidId_Fails(string entity, string message)
        {
            using var repo = TestRepo.Create();
            repo.AddEntity("Tag", Fixtures.Tag);
            var before = repo.Snapshot();

            RunResult result = Run(repo, "crud", entity);

            Assert.Equal(ExitCodes.UserError, result.ExitCode);
            Assert.Contains(message, result.Error);
            Assert.Equal(before, repo.Snapshot());
        }

        [Fact]
        public void Crud_DryRun_TouchesNothing()
        {
            using var repo = CategoryRepo();
            var before = repo.Snapshot();

            RunResult result = Run(repo, "crud", "Category", "--dry-run");

            Assert.Equal(ExitCodes.Success, result.ExitCode);
            Assert.Equal(before, repo.Snapshot());
            Assert.Contains("DRY RUN", result.Output);
            Assert.Contains("Oluşturulacak (16):", result.Output);
            foreach (string file in CategoryFiles)
                Assert.Contains($"+ {file}", result.Output);
        }
    }
}
