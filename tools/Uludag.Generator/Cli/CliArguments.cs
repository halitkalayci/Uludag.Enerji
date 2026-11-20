using System.Text.RegularExpressions;

namespace Uludag.Generator.Cli
{
    internal enum CliCommand
    {
        Help,
        Version,
        List,
        Crud
    }

    // Ayrıştırılmış komut satırı. Entity/Plural yalnızca crud için doludur.
    internal sealed record CliArguments(
        CliCommand Command,
        string? Entity = null,
        bool Force = false,
        bool DryRun = false,
        string? Plural = null,
        string? Root = null);

    // Ayrıştırma sonucu: ya Arguments ya Error doludur.
    internal sealed record CliParseResult(CliArguments? Arguments, string? Error)
    {
        public static CliParseResult Ok(CliArguments arguments) => new(arguments, null);
        public static CliParseResult Fail(string error) => new(null, error);
    }

    // Elle yazılmış ayrıştırıcı (plan 0005, R-2): 2 komut, 5 seçenek; ek kütüphane gerekmez.
    // Etkileşimli istem yoktur (spec D-2): her şey argümanla verilir, AI ajanı da çalıştırabilir.
    internal static partial class CliParser
    {
        public const string Usage = """
            uludag-gen — Domain entity'sinden CQRS CRUD dilimi üretir (spec 0005).

            Kullanım:
              uludag-gen list [--root <yol>]
              uludag-gen crud <Entity> [--dry-run] [--force] [--plural <Ad>] [--root <yol>]
              uludag-gen --version
              uludag-gen --help

            Seçenekler:
              --dry-run       Hiçbir dosyaya dokunmadan oluşacak/değişecek dosyaları listeler.
              --force         Var olan hedef dosyaların üzerine yazar.
              --plural <Ad>   Çoğul adı elle verir (varsayılan: İngilizce kuralları, ör. Category -> Categories).
              --root <yol>    Repo kökü (varsayılan: bulunulan dizinden yukarı doğru aranır).

            Çıkış kodları: 0 başarı · 1 hatalı girdi · 2 çakışma (hedef dosya var, hiçbir şey yazılmadı).
            """;

        [GeneratedRegex("^[A-Z][A-Za-z0-9]*$")]
        private static partial Regex TypeNameRegex();

        public static CliParseResult Parse(IReadOnlyList<string> args)
        {
            if (args.Count == 0 || args.Contains("--help") || args.Contains("-h"))
                return CliParseResult.Ok(new CliArguments(CliCommand.Help));
            if (args.Contains("--version"))
                return CliParseResult.Ok(new CliArguments(CliCommand.Version));

            CliCommand command;
            switch (args[0])
            {
                case "list": command = CliCommand.List; break;
                case "crud": command = CliCommand.Crud; break;
                default: return CliParseResult.Fail($"Bilinmeyen komut: '{args[0]}'. Komutlar: list, crud.");
            }

            var positionals = new List<string>();
            bool force = false, dryRun = false;
            string? plural = null, root = null;

            for (int i = 1; i < args.Count; i++)
            {
                string arg = args[i];
                switch (arg)
                {
                    case "--force": force = true; break;
                    case "--dry-run": dryRun = true; break;
                    case "--plural":
                    case "--root":
                        if (i + 1 >= args.Count || args[i + 1].StartsWith("--", StringComparison.Ordinal))
                            return CliParseResult.Fail($"'{arg}' seçeneği bir değer ister.");
                        if (arg == "--plural") plural = args[++i];
                        else root = args[++i];
                        break;
                    default:
                        if (arg.StartsWith('-'))
                            return CliParseResult.Fail($"Bilinmeyen seçenek: '{arg}'.");
                        positionals.Add(arg);
                        break;
                }
            }

            if (command == CliCommand.List)
            {
                if (positionals.Count > 0)
                    return CliParseResult.Fail($"'list' argüman almaz: '{positionals[0]}'.");
                if (force || dryRun || plural is not null)
                    return CliParseResult.Fail("'list' yalnızca --root seçeneğini alır.");
                return CliParseResult.Ok(new CliArguments(CliCommand.List, Root: root));
            }

            if (positionals.Count == 0)
                return CliParseResult.Fail("'crud' bir entity adı ister: uludag-gen crud <Entity>. Entity'leri görmek için: uludag-gen list.");
            if (positionals.Count > 1)
                return CliParseResult.Fail($"'crud' tek entity adı alır; fazladan: '{positionals[1]}'.");
            if (!TypeNameRegex().IsMatch(positionals[0]))
                return CliParseResult.Fail($"Geçersiz entity adı: '{positionals[0]}' (büyük harfle başlayan bir C# tip adı olmalı).");
            if (plural is not null && !TypeNameRegex().IsMatch(plural))
                return CliParseResult.Fail($"Geçersiz çoğul ad: '{plural}' (büyük harfle başlayan bir C# adı olmalı).");

            return CliParseResult.Ok(new CliArguments(CliCommand.Crud, positionals[0], force, dryRun, plural, root));
        }
    }
}
