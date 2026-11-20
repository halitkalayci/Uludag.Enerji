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

            var changed = before.Keys.Where(k => before[k] != after[k]).Order().ToList();
            Assert.Equal(
                [
                    "ECommerce/src/ECommerce.Persistence/Contexts/UludagDbContext.cs",
                    "ECommerce/src/ECommerce.Persistence/PersistenceServiceRegisteration.cs",
                    "ECommerce/src/ECommerce.Persistence/Seeds/OperationClaimSeeds.cs",
                ],
                changed);
            Assert.Contains("Değiştirildi (3):", result.Output);
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
        public void Configuration_EachMoney_MappedAsOwnedTypeWithPrecisionAndCurrencyLength()
        {
            // Owned-type eşlemesi olmadan kod derlenir ama EF Money'yi anahtarsız entity sanar (spec R-2, G-4).
            using var repo = TestRepo.Create();
            repo.AddEntity("Coupon", Fixtures.Coupon);

            Assert.Equal(ExitCodes.Success, Run(repo, "crud", "Coupon").ExitCode);

            string configuration = File.ReadAllText(Path.Combine(repo.Layout.PersistenceDir, "Configurations", "CouponConfiguration.cs"));
            foreach (string money in new[] { "Discount", "MinimumOrder" })
            {
                int start = configuration.IndexOf($"builder.OwnsOne(c => c.{money}, money =>", StringComparison.Ordinal);
                Assert.True(start >= 0, $"{money} için OwnsOne yok");
                int end = configuration.IndexOf($"builder.Navigation(c => c.{money}).IsRequired();", start, StringComparison.Ordinal);
                Assert.True(end > start, $"{money} için zorunlu navigation yok");
                string block = configuration[start..end];
                Assert.Contains("money.Property(x => x.Amount)", block);
                Assert.Contains(".HasPrecision(18, 2);", block);
                Assert.Contains("money.Property(x => x.Currency)", block);
                Assert.Contains(".HasMaxLength(3)", block);
                Assert.Contains(".IsRequired();", block);
            }
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
        public void Crud_Report_MigrationNameIsSnakeCase()
        {
            using var repo = TestRepo.Create();

            RunResult result = Run(repo, "crud", "OrderItem", "--dry-run");

            Assert.Contains("dotnet ef migrations add add_order_item -p ECommerce/src/ECommerce.Persistence -s ECommerce/src/ECommerce.WebAPI", result.Output);
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
        public void Crud_ExistingFile_WritesNothingExit2()
        {
            using var repo = CategoryRepo();
            string existing = repo.Full(CategoryFiles[5]); // CategoryResponse.cs elle yazılmış olsun
            repo.Write(existing, "// elle yazıldı\n");
            var before = repo.Snapshot();

            RunResult result = Run(repo, "crud", "Category");

            Assert.Equal(ExitCodes.Conflict, result.ExitCode);
            Assert.Equal(before, repo.Snapshot()); // ne yeni dosya ne kayıt eklemesi
            Assert.Contains($"= {CategoryFiles[5]}", result.Error);
            Assert.Contains("--force", result.Error);
            Assert.Contains("--force yalnızca", result.Error);
            Assert.Contains("uludag-gen'in ürettiği dosyaların üzerine yazmak içindir", result.Error);
        }

        [Fact]
        public void Crud_ExistingFile_Force_Overwrites()
        {
            using var repo = CategoryRepo();
            string existing = repo.Full(CategoryFiles[5]);
            repo.Write(existing, "// elle yazıldı\n");

            RunResult result = Run(repo, "crud", "Category", "--force");

            Assert.Equal(ExitCodes.Success, result.ExitCode);
            Assert.Contains("public class CategoryResponse", File.ReadAllText(existing));
            Assert.Contains($"! {CategoryFiles[5]}", result.Output);
            foreach (string file in CategoryFiles)
                Assert.True(File.Exists(repo.Full(file)), file);
        }

        [Fact]
        public void Crud_SecondRunWithForce_DoesNotDuplicateRegistrations()
        {
            using var repo = CategoryRepo();
            Assert.Equal(ExitCodes.Success, Run(repo, "crud", "Category").ExitCode);
            string seedsAfterFirst = File.ReadAllText(repo.Layout.OperationClaimSeedsFile);

            Assert.Equal(ExitCodes.Conflict, Run(repo, "crud", "Category").ExitCode);
            RunResult second = Run(repo, "crud", "Category", "--force");

            Assert.Equal(ExitCodes.Success, second.ExitCode);
            Assert.Contains("Zaten kayıtlı (atlandı):", second.Output);
            Assert.DoesNotContain("Değiştirildi", second.Output);
            Assert.Equal(seedsAfterFirst, File.ReadAllText(repo.Layout.OperationClaimSeedsFile)); // seed Id'leri sabit kalır
        }

        [Fact]
        public void Crud_MissingRegistrationFile_FailsAndWritesNothing()
        {
            using var repo = TestRepo.Create(withRegistrationFiles: false);
            repo.AddEntity("Category", Fixtures.Category);
            var before = repo.Snapshot();

            RunResult result = Run(repo, "crud", "Category");

            Assert.Equal(ExitCodes.UserError, result.ExitCode);
            Assert.Contains("kayıt dosyası bulunamadı", result.Error);
            Assert.Equal(before, repo.Snapshot());
        }

        [Fact]
        public void Crud_DryRun_WithConflict_ListsPlanAndTouchesNothing()
        {
            using var repo = CategoryRepo();
            repo.Write(repo.Full(CategoryFiles[5]), "// elle yazıldı\n");
            var before = repo.Snapshot();

            RunResult result = Run(repo, "crud", "Category", "--dry-run");

            Assert.Equal(ExitCodes.Conflict, result.ExitCode);
            Assert.Equal(before, repo.Snapshot());
            Assert.Contains("Oluşturulacak (15):", result.Output);
            Assert.Contains("Çakışan (zaten var, yazılmayacak) (1):", result.Output);
            Assert.Contains($"! {CategoryFiles[5]}", result.Output);
            Assert.Contains("Değiştirilecek (3):", result.Output);
            Assert.Contains($"= {CategoryFiles[5]}", result.Error);
            Assert.DoesNotContain("--dry-run kullanın", result.Error); // zaten verilmiş
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
