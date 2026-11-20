using Uludag.Generator.Tests.Infrastructure;
using static Uludag.Generator.Tests.Infrastructure.Generated;

namespace Uludag.Generator.Tests
{
    // AC-2 (dosyalar), AC-6 (özellik eşleme), AC-7 (çakışma/force), AC-8 (hatalı girdi, dry-run).
    public class CrudGeneratorTests
    {
        public static readonly string[] TopicFiles =
        [
            "ECommerce/src/ECommerce.Application/Features/Topic/Commands/Create/CreateTopicCommand.cs",
            "ECommerce/src/ECommerce.Application/Features/Topic/Commands/Update/UpdateTopicCommand.cs",
            "ECommerce/src/ECommerce.Application/Features/Topic/Commands/Delete/DeleteTopicCommand.cs",
            "ECommerce/src/ECommerce.Application/Features/Topic/Queries/GetAll/GetAllTopicQuery.cs",
            "ECommerce/src/ECommerce.Application/Features/Topic/Queries/GetById/GetByIdTopicQuery.cs",
            "ECommerce/src/ECommerce.Application/Features/Topic/Queries/TopicResponse.cs",
            "ECommerce/src/ECommerce.Application/Features/Topic/Rules/TopicBusinessRules.cs",
            "ECommerce/src/ECommerce.Application/Features/Topic/Mappers/TopicMapper.cs",
            "ECommerce/src/ECommerce.Application/Features/Topic/Constants/TopicOperationClaims.cs",
            "ECommerce/src/ECommerce.Application/Features/Topic/Constants/TopicConstants.cs",
            "ECommerce/src/ECommerce.Application/Features/Topic/Locales/TopicLocale.tr.yaml",
            "ECommerce/src/ECommerce.Application/Features/Topic/Locales/TopicLocale.en.yaml",
            "ECommerce/src/ECommerce.Application/Services/Repositories/ITopicRepository.cs",
            "ECommerce/src/ECommerce.Persistence/Repositories/TopicRepository.cs",
            "ECommerce/src/ECommerce.Persistence/Configurations/TopicConfiguration.cs",
            "ECommerce/src/ECommerce.WebAPI/Controllers/TopicsController.cs",
        ];

        private static TestRepo TopicRepo()
        {
            var repo = TestRepo.Create();
            repo.AddEntity("Topic", Fixtures.Topic);
            return repo;
        }

        [Fact]
        public void Crud_FixtureEntity_Writes16FilesAnd3Registrations()
        {
            using var repo = TopicRepo();
            var before = repo.Snapshot();

            RunResult result = Run(repo, "crud", "Topic");

            Assert.Equal(ExitCodes.Success, result.ExitCode);
            var after = repo.Snapshot();
            var added = after.Keys.Except(before.Keys).Order().ToList();
            Assert.Equal(TopicFiles.Order(), added);
            foreach (string file in TopicFiles)
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
            Assert.Contains("dotnet ef migrations add add_topic", result.Output);
        }

        [Fact]
        public void Map_Id_NotInCreate_RouteFieldInUpdate()
        {
            using var repo = TopicRepo();
            Run(repo, "crud", "Topic");

            var create = Properties(FeatureFile(repo, "Topic", "Commands/Create/CreateTopicCommand.cs"), "CreateTopicCommand");
            var update = Properties(FeatureFile(repo, "Topic", "Commands/Update/UpdateTopicCommand.cs"), "UpdateTopicCommand");
            var response = Properties(FeatureFile(repo, "Topic", "Queries/TopicResponse.cs"), "TopicResponse");

            Assert.DoesNotContain("Id", create.Keys);
            Assert.Equal("Guid", update["Id"]);
            Assert.Equal("Guid", response["Id"]);
        }

        [Fact]
        public void Map_SingleMoney_FlattenedToAmountAndCurrency()
        {
            using var repo = TopicRepo();
            Run(repo, "crud", "Topic");

            var create = Properties(FeatureFile(repo, "Topic", "Commands/Create/CreateTopicCommand.cs"), "CreateTopicCommand");
            string mapper = File.ReadAllText(FeatureFile(repo, "Topic", "Mappers/TopicMapper.cs"));

            Assert.Equal("decimal", create["Amount"]);
            Assert.Equal("string", create["Currency"]);
            Assert.DoesNotContain("MinimumPrice", create.Keys);
            Assert.Contains("MinimumPrice = new Domain.ValueObjects.Money(command.Amount, command.Currency)", mapper);
            Assert.Contains("Amount = topic.MinimumPrice.Amount", mapper);
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

        [Theory]
        [InlineData("OrderItem", "add_order_item")]
        [InlineData("OTPCode", "add_otp_code")]   // kısaltma bölünmez
        [InlineData("Product", "add_product")]
        public void Crud_Report_MigrationNameIsSnakeCase(string entity, string migration)
        {
            using var repo = TestRepo.Create();
            repo.AddEntity("OTPCode", "namespace ECommerce.Domain.Entities { public class OTPCode { public Guid Id { get; set; } } }");

            RunResult result = Run(repo, "crud", entity, "--dry-run");

            Assert.Contains($"dotnet ef migrations add {migration} -p ECommerce/src/ECommerce.Persistence -s ECommerce/src/ECommerce.WebAPI", result.Output);
        }

        [Fact]
        public void Map_NavigationAndCollection_SkippedAndReported()
        {
            using var repo = TopicRepo();

            RunResult result = Run(repo, "crud", "Topic");

            var response = Properties(FeatureFile(repo, "Topic", "Queries/TopicResponse.cs"), "TopicResponse");
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
            using var repo = TopicRepo();
            Run(repo, "crud", "Topic");

            var create = Properties(FeatureFile(repo, "Topic", "Commands/Create/CreateTopicCommand.cs"), "CreateTopicCommand");

            Assert.Equal("Guid?", create["ParentTopicId"]);
            Assert.Equal("string?", create["Description"]);
            Assert.Equal("DateTime?", create["PublishedAt"]);
            Assert.Equal("TopicStatus", create["Status"]);
            Assert.Equal("string", create["Name"]);
        }

        [Fact]
        public void Map_UserId_NotInCommands_InResponse_Warned()
        {
            using var repo = TopicRepo();

            RunResult result = Run(repo, "crud", "Topic");

            var create = Properties(FeatureFile(repo, "Topic", "Commands/Create/CreateTopicCommand.cs"), "CreateTopicCommand");
            var update = Properties(FeatureFile(repo, "Topic", "Commands/Update/UpdateTopicCommand.cs"), "UpdateTopicCommand");
            var response = Properties(FeatureFile(repo, "Topic", "Queries/TopicResponse.cs"), "TopicResponse");
            Assert.DoesNotContain("UserId", create.Keys);
            Assert.DoesNotContain("UserId", update.Keys);
            Assert.Equal("Guid", response["UserId"]);
            Assert.Contains("! UserId — kimliğe bağlı alan", result.Output);
        }

        [Fact]
        public void Crud_SmokeFixture_ClientSuppliedForeignKeyInCommands_NoIdentityWarning()
        {
            // Çalışırlık gözlemi fixture'ı: tüm zorunlu alanlar istemciden gelir, elle düzenleme gerekmez (M-1).
            using var repo = TestRepo.Create();
            repo.AddEntity("PriceAlert", Fixtures.PriceAlert);

            RunResult result = Run(repo, "crud", "PriceAlert");

            Assert.Equal(ExitCodes.Success, result.ExitCode);
            var create = Properties(FeatureFile(repo, "PriceAlert", "Commands/Create/CreatePriceAlertCommand.cs"), "CreatePriceAlertCommand");
            Assert.Equal(["Label", "Priority", "IsActive", "Amount", "Currency", "ProductId"], create.Keys);
            Assert.DoesNotContain("Uyarılar:", result.Output);
        }

        [Fact]
        public void Crud_PluralOverride_UsedForControllerAndDbSet()
        {
            using var repo = TopicRepo();

            Run(repo, "crud", "Topic", "--plural", "TopicList");

            Assert.True(File.Exists(Path.Combine(repo.Layout.WebApiDir, "Controllers", "TopicListController.cs")));
            Assert.Contains("uludagDbContext.TopicList", File.ReadAllText(Path.Combine(repo.Layout.PersistenceDir, "Repositories", "TopicRepository.cs")));
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
            using var repo = TopicRepo();
            string existing = repo.Full(TopicFiles[5]); // TopicResponse.cs elle yazılmış olsun
            repo.Write(existing, "// elle yazıldı\n");
            var before = repo.Snapshot();

            RunResult result = Run(repo, "crud", "Topic");

            Assert.Equal(ExitCodes.Conflict, result.ExitCode);
            Assert.Equal(before, repo.Snapshot()); // ne yeni dosya ne kayıt eklemesi
            Assert.Contains($"= {TopicFiles[5]}", result.Error);
            Assert.Contains("--force", result.Error);
            Assert.Contains("--force yalnızca", result.Error);
            Assert.Contains("uludag-gen'in ürettiği dosyaların üzerine yazmak içindir", result.Error);
        }

        [Fact]
        public void Crud_ExistingFile_Force_Overwrites()
        {
            using var repo = TopicRepo();
            string existing = repo.Full(TopicFiles[5]);
            repo.Write(existing, "// elle yazıldı\n");

            RunResult result = Run(repo, "crud", "Topic", "--force");

            Assert.Equal(ExitCodes.Success, result.ExitCode);
            Assert.Contains("public class TopicResponse", File.ReadAllText(existing));
            Assert.Contains($"! {TopicFiles[5]}", result.Output);
            foreach (string file in TopicFiles)
                Assert.True(File.Exists(repo.Full(file)), file);
        }

        [Fact]
        public void Crud_SecondRunWithForce_DoesNotDuplicateRegistrations()
        {
            using var repo = TopicRepo();
            Assert.Equal(ExitCodes.Success, Run(repo, "crud", "Topic").ExitCode);
            string seedsAfterFirst = File.ReadAllText(repo.Layout.OperationClaimSeedsFile);

            Assert.Equal(ExitCodes.Conflict, Run(repo, "crud", "Topic").ExitCode);
            RunResult second = Run(repo, "crud", "Topic", "--force");

            Assert.Equal(ExitCodes.Success, second.ExitCode);
            Assert.Contains("Zaten kayıtlı (atlandı):", second.Output);
            Assert.DoesNotContain("Değiştirildi", second.Output);
            Assert.Equal(seedsAfterFirst, File.ReadAllText(repo.Layout.OperationClaimSeedsFile)); // seed Id'leri sabit kalır
        }

        [Fact]
        public void Crud_MissingRegistrationFile_FailsAndWritesNothing()
        {
            using var repo = TestRepo.Create(withRegistrationFiles: false);
            repo.AddEntity("Topic", Fixtures.Topic);
            var before = repo.Snapshot();

            RunResult result = Run(repo, "crud", "Topic");

            Assert.Equal(ExitCodes.UserError, result.ExitCode);
            Assert.Contains("kayıt dosyası bulunamadı", result.Error);
            Assert.Equal(before, repo.Snapshot());
        }

        [Fact]
        public void Crud_DryRun_WithConflict_ListsPlanAndTouchesNothing()
        {
            using var repo = TopicRepo();
            repo.Write(repo.Full(TopicFiles[5]), "// elle yazıldı\n");
            var before = repo.Snapshot();

            RunResult result = Run(repo, "crud", "Topic", "--dry-run");

            Assert.Equal(ExitCodes.Conflict, result.ExitCode);
            Assert.Equal(before, repo.Snapshot());
            Assert.Contains("Oluşturulacak (15):", result.Output);
            Assert.Contains("ÇAKIŞMA VAR: --force olmadan hiçbir dosya yazılmaz", result.Output);
            Assert.Contains("Üzerine yazılacak (yalnızca --force ile) (1):", result.Output);
            Assert.Contains($"! {TopicFiles[5]}", result.Output);
            Assert.Contains("Değiştirilecek (3):", result.Output);
            Assert.Contains($"= {TopicFiles[5]}", result.Error);
            Assert.DoesNotContain("--dry-run kullanın", result.Error); // zaten verilmiş
        }

        [Fact]
        public void Crud_DryRun_TouchesNothing()
        {
            using var repo = TopicRepo();
            var before = repo.Snapshot();

            RunResult result = Run(repo, "crud", "Topic", "--dry-run");

            Assert.Equal(ExitCodes.Success, result.ExitCode);
            Assert.Equal(before, repo.Snapshot());
            Assert.Contains("DRY RUN", result.Output);
            Assert.DoesNotContain("ÇAKIŞMA VAR", result.Output);
            Assert.Contains("Oluşturulacak (16):", result.Output);
            foreach (string file in TopicFiles)
                Assert.Contains($"+ {file}", result.Output);
        }
    }
}
