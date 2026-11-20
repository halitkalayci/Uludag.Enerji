namespace Uludag.Generator.Workspace
{
    // Repo düzeni (spec D-3: yalnızca bu repo). Tüm yollar kökten türetilir.
    internal sealed record RepoLayout(string Root)
    {
        public const string MarkerRelativePath = "ECommerce/src/ECommerce.Domain/Entities";

        public string SrcDir => Path.Combine(Root, "ECommerce", "src");
        public string DomainDir => Path.Combine(SrcDir, "ECommerce.Domain");
        public string EntitiesDir => Path.Combine(DomainDir, "Entities");
        public string ValueObjectsDir => Path.Combine(DomainDir, "ValueObjects");
        public string ApplicationDir => Path.Combine(SrcDir, "ECommerce.Application");
        public string PersistenceDir => Path.Combine(SrcDir, "ECommerce.Persistence");
        public string WebApiDir => Path.Combine(SrcDir, "ECommerce.WebAPI");

        public string DbContextFile => Path.Combine(PersistenceDir, "Contexts", "UludagDbContext.cs");
        public string PersistenceRegistrationFile => Path.Combine(PersistenceDir, "PersistenceServiceRegisteration.cs");
        public string OperationClaimSeedsFile => Path.Combine(PersistenceDir, "Seeds", "OperationClaimSeeds.cs");

        // Çıktıda kökten göreli, '/' ayraçlı yol.
        public string Relative(string path) => Path.GetRelativePath(Root, path).Replace('\\', '/');
    }

    // Repo kökünü bulur: --root verilmişse onu doğrular, yoksa bulunulan dizinden yukarı doğru
    // ECommerce/src/ECommerce.Domain/Entities klasörünü arar (spec R-8).
    internal static class RepoLocator
    {
        public static RepoLayout? Locate(string workingDirectory, string? rootOverride)
        {
            if (rootOverride is not null)
            {
                string root = Path.GetFullPath(rootOverride, workingDirectory);
                return IsRepoRoot(root) ? new RepoLayout(root) : null;
            }

            for (DirectoryInfo? dir = new(Path.GetFullPath(workingDirectory)); dir is not null; dir = dir.Parent)
                if (IsRepoRoot(dir.FullName))
                    return new RepoLayout(dir.FullName);

            return null;
        }

        private static bool IsRepoRoot(string path) => Directory.Exists(Path.Combine(path, RepoLayout.MarkerRelativePath));
    }
}
