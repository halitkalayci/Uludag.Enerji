using Uludag.Generator.Tests.Infrastructure;
using Uludag.Generator.Workspace;

namespace Uludag.Generator.Tests
{
    // Spec R-8 (repo kökünü kendisi bulur) ve AC-8 (repo dışında çalıştırma).
    public class RepoLocatorTests
    {
        [Fact]
        public void Locate_FromNestedDirectory_FindsRoot()
        {
            using var repo = TestRepo.Create();

            RepoLayout? layout = RepoLocator.Locate(repo.Layout.EntitiesDir, null);

            Assert.Equal(Path.GetFullPath(repo.Root), layout?.Root);
        }

        [Fact]
        public void Locate_RootOverride_UsesIt()
        {
            using var repo = TestRepo.Create();

            RepoLayout? layout = RepoLocator.Locate(Path.GetTempPath(), repo.Root);

            Assert.Equal(Path.GetFullPath(repo.Root), layout?.Root);
        }

        [Fact]
        public void Locate_OutsideRepo_Fails()
        {
            string outside = TestRepo.NewTempDirectory();
            try
            {
                var error = new StringWriter();

                Assert.Null(RepoLocator.Locate(outside, null));
                Assert.Null(RepoLocator.Locate(Path.GetTempPath(), outside)); // geçersiz --root
                Assert.Equal(ExitCodes.UserError, App.Run(["list"], outside, new StringWriter(), error));
                Assert.Contains("repo kökü bulunamadı", error.ToString());
                Assert.Empty(Directory.EnumerateFileSystemEntries(outside));
            }
            finally
            {
                Directory.Delete(outside, recursive: true);
            }
        }
    }
}
