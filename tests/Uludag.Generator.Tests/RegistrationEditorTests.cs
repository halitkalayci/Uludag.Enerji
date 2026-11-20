using System.Text;
using System.Text.RegularExpressions;
using Uludag.Generator.Analysis;
using Uludag.Generator.Generation;
using Uludag.Generator.Tests.Infrastructure;

namespace Uludag.Generator.Tests
{
    // Spec R-3 (kayıtlar) ve AC-7 (idempotentlik). Testler gerçek kayıt dosyalarının kopyası üzerinde koşar.
    public class RegistrationEditorTests
    {
        private static (TestRepo Repo, CrudModel Model) Setup()
        {
            var repo = TestRepo.Create();
            repo.AddEntity("Topic", Fixtures.Topic);
            DomainModel domain = EntityReader.Read(repo.Layout);
            return (repo, new CrudModel(domain.FindEntity("Topic")!, "Topics", domain.Enums));
        }

        private static void Apply(RegistrationOutcome outcome)
        {
            foreach (PlannedEdit edit in outcome.Edits)
                File.WriteAllText(edit.Path, edit.Content, new UTF8Encoding(edit.Bom));
        }

        [Fact]
        public void Apply_AddsDbSetRepositoryAndSixClaims()
        {
            var (repo, model) = Setup();
            using (repo)
            {
                RegistrationOutcome outcome = RegistrationEditor.Plan(repo.Layout, model, Guid.NewGuid);
                Apply(outcome);

                Assert.Null(outcome.Error);
                Assert.Contains("        public DbSet<Topic> Topics => Set<Topic>();", File.ReadAllText(repo.Layout.DbContextFile));
                Assert.Contains("            services.AddScoped<ITopicRepository, TopicRepository>();", File.ReadAllText(repo.Layout.PersistenceRegistrationFile));

                string seeds = File.ReadAllText(repo.Layout.OperationClaimSeedsFile);
                Assert.Contains("using ECommerce.Application.Features.Topic.Constants;", seeds);
                var ids = Regex.Matches(seeds, @"Guid\.Parse\(""([0-9a-f-]{36})""\), Name = TopicOperationClaims\.(\w+) \}")
                    .Select(m => (Id: m.Groups[1].Value, Action: m.Groups[2].Value)).ToList();
                Assert.Equal(["Admin", "Write", "Read", "Create", "Update", "Delete"], ids.Select(x => x.Action));
                Assert.Equal(6, ids.Select(x => x.Id).Distinct().Count());
                // Yeni claim'ler dizinin içinde, kapanıştan önce.
                Assert.True(seeds.IndexOf("TopicOperationClaims.Delete", StringComparison.Ordinal) < seeds.LastIndexOf("];", StringComparison.Ordinal));
            }
        }

        [Fact]
        public void Apply_Twice_NoDuplicates()
        {
            var (repo, model) = Setup();
            using (repo)
            {
                Apply(RegistrationEditor.Plan(repo.Layout, model, Guid.NewGuid));
                var afterFirst = repo.Snapshot();

                RegistrationOutcome second = RegistrationEditor.Plan(repo.Layout, model, Guid.NewGuid);
                Apply(second);

                Assert.Empty(second.Edits);
                Assert.Equal(3, second.AlreadyRegistered.Count);
                Assert.Equal(afterFirst, repo.Snapshot());
            }
        }

        [Fact]
        public void Apply_CrlfAndBom_Preserved()
        {
            var (repo, model) = Setup();
            using (repo)
            {
                string path = repo.Layout.DbContextFile;
                string crlf = File.ReadAllText(path).Replace("\r\n", "\n").Replace("\n", "\r\n");
                File.WriteAllText(path, crlf, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

                Apply(RegistrationEditor.Plan(repo.Layout, model, Guid.NewGuid));

                byte[] bytes = File.ReadAllBytes(path);
                string text = Encoding.UTF8.GetString(bytes);
                Assert.Equal([0xEF, 0xBB, 0xBF], bytes[..3]);
                Assert.DoesNotMatch("[^\r]\n", text);
                Assert.Contains("public DbSet<Topic> Topics => Set<Topic>();\r\n", text);
            }
        }

        [Fact]
        public void Plan_AnchorMissing_ReturnsErrorAndNoEdits()
        {
            var (repo, model) = Setup();
            using (repo)
            {
                File.WriteAllText(repo.Layout.PersistenceRegistrationFile, "// DI kayıtları taşındı\n");

                RegistrationOutcome outcome = RegistrationEditor.Plan(repo.Layout, model, Guid.NewGuid);

                Assert.Empty(outcome.Edits);
                Assert.Contains("PersistenceServiceRegisteration.cs içinde", outcome.Error);
                Assert.Contains("bulunamadı", outcome.Error);
            }
        }
    }
}
