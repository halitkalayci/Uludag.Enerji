using Uludag.Generator.Analysis;
using Uludag.Generator.Tests.Infrastructure;

namespace Uludag.Generator.Tests
{
    // AC-1 (liste) ve AC-6'nın okuma kısmı (özellik sınıflandırması).
    public class EntityReaderTests
    {
        [Fact]
        public void List_DomainEntities_ExcludesEnumsAndValueObjects()
        {
            var output = new StringWriter();

            int exitCode = App.Run(["list"], TestRepo.RealRepo.Root, output, new StringWriter());

            string[] names = output.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(ExitCodes.Success, exitCode);
            Assert.Contains("Product", names);
            Assert.Contains("Order", names);
            Assert.Contains("OrderItem", names);
            Assert.DoesNotContain("RefreshTokenRevocationReason", names); // enum
            Assert.DoesNotContain("Money", names);                        // value object
        }

        [Fact]
        public void Read_UnbuildableSource_StillParses()
        {
            using var repo = TestRepo.Create();
            repo.AddEntity("Draft", Fixtures.Broken);

            DomainModel domain = EntityReader.Read(repo.Layout);

            EntityModel? draft = domain.FindEntity("Draft");
            Assert.NotNull(draft);
            Assert.True(draft.HasGuidId);
            Assert.Equal(PropertyKind.Unknown, draft.Properties.Single(p => p.Name == "Thing").Kind);
        }

        [Fact]
        public void Read_FixtureEntity_ClassifiesEveryProperty()
        {
            using var repo = TestRepo.Create();
            repo.AddEntity("Category", Fixtures.Category);

            EntityModel category = EntityReader.Read(repo.Layout).FindEntity("Category")!;

            var kinds = category.Properties.ToDictionary(p => p.Name, p => (p.Kind, p.IsNullable));
            Assert.Equal((PropertyKind.Id, false), kinds["Id"]);
            Assert.Equal((PropertyKind.Scalar, false), kinds["Name"]);
            Assert.Equal((PropertyKind.Scalar, true), kinds["Description"]);
            Assert.Equal((PropertyKind.Scalar, false), kinds["DisplayOrder"]);
            Assert.Equal((PropertyKind.Scalar, false), kinds["IsActive"]);
            Assert.Equal((PropertyKind.Scalar, true), kinds["PublishedAt"]);
            Assert.Equal((PropertyKind.Scalar, false), kinds["Status"]);          // enum
            Assert.Equal((PropertyKind.Money, false), kinds["MinimumPrice"]);
            Assert.Equal((PropertyKind.Scalar, false), kinds["UserId"]);
            Assert.Equal((PropertyKind.Scalar, true), kinds["ParentCategoryId"]);
            Assert.Equal((PropertyKind.Unknown, true), kinds["Image"]);          // dizi
            Assert.Equal((PropertyKind.Navigation, false), kinds["User"]);
            Assert.Equal((PropertyKind.Collection, false), kinds["Products"]);
            Assert.False(kinds.ContainsKey("DisplayName")); // hesaplanan (=>) özellik
            Assert.False(kinds.ContainsKey("Counter"));     // static
            Assert.True(category.Properties.Single(p => p.Name == "UserId").IsIdentityBound);
        }

        [Fact]
        public void Read_EnumInEntitiesFolder_IsNotAnEntity()
        {
            using var repo = TestRepo.Create();
            repo.AddEntity("Category", Fixtures.Category);

            DomainModel domain = EntityReader.Read(repo.Layout);

            Assert.Null(domain.FindEntity("CategoryStatus"));
            Assert.Equal("ECommerce.Domain.Entities", domain.Enums["CategoryStatus"]);
            Assert.Contains("Money", domain.ValueObjects);
        }

        [Fact]
        public void Read_IntId_HasNoGuidId()
        {
            using var repo = TestRepo.Create();
            repo.AddEntity("Tag", Fixtures.Tag);

            Assert.False(EntityReader.Read(repo.Layout).FindEntity("Tag")!.HasGuidId);
        }
    }
}
