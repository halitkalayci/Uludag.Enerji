using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Uludag.Generator.Tests.Infrastructure
{
    // Üretilen dosyaları okuyan yardımcılar: aracı çalıştırır, sonucu Roslyn ile inceler.
    internal static class Generated
    {
        public sealed record RunResult(int ExitCode, string Output, string Error);

        public static RunResult Run(TestRepo repo, params string[] args)
        {
            var output = new StringWriter();
            var error = new StringWriter();
            int exitCode = App.Run([.. args, "--root", repo.Root], repo.Root, output, error);
            return new RunResult(exitCode, output.ToString(), error.ToString());
        }

        public static string FeatureFile(TestRepo repo, string entity, string relative) =>
            Path.Combine(repo.Layout.ApplicationDir, "Features", entity, relative);

        public static CompilationUnitSyntax Parse(string path) =>
            CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetCompilationUnitRoot();

        public static ClassDeclarationSyntax Class(string path, string className) =>
            Parse(path).DescendantNodes().OfType<ClassDeclarationSyntax>().Single(c => c.Identifier.Text == className);

        // Sınıfın kendi public özellikleri: ad → tip (iç sınıflarınki hariç).
        public static Dictionary<string, string> Properties(string path, string className) =>
            Class(path, className).Members.OfType<PropertyDeclarationSyntax>()
                .Where(p => p.ExplicitInterfaceSpecifier is null)
                .ToDictionary(p => p.Identifier.Text, p => p.Type.ToString());
    }
}
