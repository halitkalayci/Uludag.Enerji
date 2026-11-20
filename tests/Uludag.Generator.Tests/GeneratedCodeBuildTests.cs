using System.Diagnostics;
using Uludag.Generator.Tests.Infrastructure;
using Uludag.Generator.Workspace;

namespace Uludag.Generator.Tests
{
    // AC-2: üretilen dilim gerçek çözümün içinde derlenir. Kaynak kodun kopyası repo DIŞINDA (geçici dizin)
    // kurulur, fixture entity eklenir, araç çalıştırılır ve WebAPI (tüm katmanlar) derlenir. Uzun sürdüğü için
    // full profilde koşar (plan 0005, R-5); kopya test sonunda silinir.
    [Trait("Category", "Integration")]
    public class GeneratedCodeBuildTests
    {
        private static readonly HashSet<string> SkippedDirectories = new(StringComparer.OrdinalIgnoreCase) { "bin", "obj", "node_modules", ".vs" };

        [Fact]
        public void Crud_FixtureEntity_GeneratedSolutionBuilds()
        {
            RepoLayout real = TestRepo.RealRepo;
            string root = TestRepo.NewTempDirectory();
            try
            {
                CopyTree(real.SrcDir, Path.Combine(root, "ECommerce", "src"));
                CopyTree(Path.Combine(real.Root, "Uludag.Core"), Path.Combine(root, "Uludag.Core"));
                File.Copy(Path.Combine(real.Root, "Directory.Build.props"), Path.Combine(root, "Directory.Build.props"));
                File.Copy(Path.Combine(real.Root, "BannedSymbols.txt"), Path.Combine(root, "BannedSymbols.txt"));
                var layout = new RepoLayout(root);
                File.WriteAllText(Path.Combine(layout.EntitiesDir, "Category.cs"), Fixtures.Category);
                File.WriteAllText(Path.Combine(layout.EntitiesDir, "Coupon.cs"), Fixtures.Coupon);
                File.WriteAllText(Path.Combine(layout.EntitiesDir, "PriceAlert.cs"), Fixtures.PriceAlert);

                foreach (string entity in new[] { "Category", "Coupon", "PriceAlert" })
                {
                    var error = new StringWriter();
                    int exitCode = App.Run(["crud", entity], root, new StringWriter(), error);
                    Assert.True(exitCode == ExitCodes.Success, $"{entity}: {error}");
                }

                string webApi = Path.Combine(layout.WebApiDir, "ECommerce.WebAPI.csproj");
                (int buildExit, string buildOutput) = RunDotnet(root, "build", webApi, "-nologo", "-v:q", "-nodeReuse:false", "-p:UseSharedCompilation=false");

                Assert.True(buildExit == 0, buildOutput);
                // Üretilen dosyalar yeni uyarı da getirmez (mevcut uyarılar başka dosyalarda).
                var generatedWarnings = buildOutput.Split('\n')
                    .Where(l => l.Contains("warning", StringComparison.OrdinalIgnoreCase))
                    .Where(l => l.Contains("Category", StringComparison.Ordinal) || l.Contains("Coupon", StringComparison.Ordinal)
                        || l.Contains("PriceAlert", StringComparison.Ordinal))
                    .Distinct()
                    .ToList();
                Assert.True(generatedWarnings.Count == 0, string.Join("\n", generatedWarnings));
            }
            finally
            {
                try { Directory.Delete(root, recursive: true); }
                catch (IOException) { /* geçici dizin; işletim sistemi temizler */ }
                catch (UnauthorizedAccessException) { }
            }
        }

        private static (int ExitCode, string Output) RunDotnet(string workingDirectory, params string[] args)
        {
            var start = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            foreach (string arg in args)
                start.ArgumentList.Add(arg);
            // dotnet test'in kendi MSBuild ortam değişkenleri iç içe derlemeyi şaşırtmasın.
            foreach (string key in start.Environment.Keys.Where(k => k.StartsWith("MSBuild", StringComparison.OrdinalIgnoreCase)).ToList())
                start.Environment.Remove(key);
            start.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";

            using var process = Process.Start(start)!;
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(TimeSpan.FromMinutes(5)))
            {
                process.Kill(entireProcessTree: true);
                return (-1, "dotnet build 5 dakikada bitmedi.");
            }
            return (process.ExitCode, stdout.Result + stderr.Result);
        }

        private static void CopyTree(string source, string target)
        {
            Directory.CreateDirectory(target);
            foreach (string file in Directory.EnumerateFiles(source))
                File.Copy(file, Path.Combine(target, Path.GetFileName(file)));
            foreach (string dir in Directory.EnumerateDirectories(source))
                if (!SkippedDirectories.Contains(Path.GetFileName(dir)))
                    CopyTree(dir, Path.Combine(target, Path.GetFileName(dir)));
        }
    }
}
