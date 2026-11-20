using Uludag.Generator.Workspace;

namespace Uludag.Generator.Tests.Infrastructure
{
    // Repo dışında (Path.GetTempPath) minimal bir repo düzeni kurar: gerçek repodan Domain (Entities,
    // ValueObjects) ve üç kayıt dosyasının kopyası. Gerçek repo dosyaları değişirse testler bunu görür
    // (plan 0005, R-4). Testin sonunda silinir; çalışma ağacının içine asla kopya açılmaz.
    internal sealed class TestRepo : IDisposable
    {
        public RepoLayout Layout { get; }
        public string Root => Layout.Root;

        private TestRepo(string root) => Layout = new RepoLayout(root);

        // Testlerin üzerinde çalıştığı gerçek repo (salt okunur kullanılır).
        public static RepoLayout RealRepo =>
            RepoLocator.Locate(AppContext.BaseDirectory, null)
            ?? throw new InvalidOperationException("Testler repo içinden koşmalı.");

        public static string NewTempDirectory()
        {
            string dir = Path.Combine(Path.GetTempPath(), "uludag-gen-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            return dir;
        }

        public static TestRepo Create(bool withRegistrationFiles = true)
        {
            var repo = new TestRepo(NewTempDirectory());
            RepoLayout real = RealRepo;
            CopyDirectory(real.EntitiesDir, repo.Layout.EntitiesDir);
            CopyDirectory(real.ValueObjectsDir, repo.Layout.ValueObjectsDir);
            if (withRegistrationFiles)
            {
                CopyFile(real.DbContextFile, repo.Layout.DbContextFile);
                CopyFile(real.PersistenceRegistrationFile, repo.Layout.PersistenceRegistrationFile);
                CopyFile(real.OperationClaimSeedsFile, repo.Layout.OperationClaimSeedsFile);
            }
            return repo;
        }

        public void AddEntity(string name, string source) => Write(Path.Combine(Layout.EntitiesDir, name + ".cs"), source);

        public string Full(string relative) => Path.Combine(Root, relative);

        public void Write(string fullPath, string content)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllText(fullPath, content);
        }

        // Kökten göreli tüm dosyalar ve içerikleri: "hiçbir şeye dokunulmadı" karşılaştırması için.
        public Dictionary<string, string> Snapshot() =>
            Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories)
                .ToDictionary(f => Layout.Relative(f), File.ReadAllText);

        public void Dispose()
        {
            try { Directory.Delete(Root, recursive: true); }
            catch (IOException) { /* geçici dizin; işletim sistemi temizler */ }
        }

        private static void CopyDirectory(string source, string target)
        {
            foreach (string file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
                CopyFile(file, Path.Combine(target, Path.GetRelativePath(source, file)));
        }

        private static void CopyFile(string source, string target)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(source, target, overwrite: true);
        }
    }
}
