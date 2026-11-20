using System.Text;
using System.Text.RegularExpressions;
using Uludag.Generator.Workspace;

namespace Uludag.Generator.Generation
{
    internal sealed record RegistrationOutcome(IReadOnlyList<PlannedEdit> Edits, IReadOnlyList<string> AlreadyRegistered, string? Error);

    // Üretilen parçaları sisteme bağlar (spec R-3): DbSet, repository DI kaydı, operation claim seed'leri.
    // Çapa (anchor) tabanlıdır: var olan satırların sonuna aynı biçimde ekler. Zaten kayıtlıysa dokunmaz
    // (idempotent). Çapa bulunamazsa hata döner ve hiçbir dosya yazılmaz (plan 0005, R-4).
    // Dosyanın satır sonu (LF/CRLF) ve BOM'u korunur.
    internal static class RegistrationEditor
    {
        private static readonly string[] ClaimActions = ["Admin", "Write", "Read", "Create", "Update", "Delete"];

        public static RegistrationOutcome Plan(RepoLayout layout, CrudModel m, Func<Guid> newGuid)
        {
            var edits = new List<PlannedEdit>();
            var already = new List<string>();
            string e = m.Entity;

            var steps = new (string Path, string Present, string Description, Func<List<string>, string?> Apply)[]
            {
                (layout.DbContextFile, $@"DbSet<\s*{e}\s*>", $"DbSet<{e}> {m.Plural}",
                    lines => InsertAfterLast(lines, @"^\s*public\s+DbSet<", indent => $"{indent}public DbSet<{e}> {m.Plural} => Set<{e}>();",
                        "'public DbSet<...>' satırı")),
                (layout.PersistenceRegistrationFile, $@"AddScoped<\s*I{e}Repository\b", $"AddScoped<I{e}Repository, {e}Repository>",
                    lines => InsertAfterLast(lines, @"^\s*services\.AddScoped<\s*I\w+Repository\s*,", indent => $"{indent}services.AddScoped<I{e}Repository, {e}Repository>();",
                        "'services.AddScoped<I...Repository, ...>' satırı")),
                (layout.OperationClaimSeedsFile, $@"\b{e}OperationClaims\.", $"{ClaimActions.Length} operation claim seed'i ({e}.*)",
                    lines => AddSeeds(lines, e, newGuid)),
            };

            foreach (var step in steps)
            {
                if (!File.Exists(step.Path))
                    return Fail($"kayıt dosyası bulunamadı: {layout.Relative(step.Path)}");

                byte[] bytes = File.ReadAllBytes(step.Path);
                bool bom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
                string text = new UTF8Encoding(false).GetString(bytes, bom ? 3 : 0, bytes.Length - (bom ? 3 : 0));
                string newline = text.Contains("\r\n") ? "\r\n" : "\n";
                string label = $"{Path.GetFileName(step.Path)}: {step.Description}";

                if (Regex.IsMatch(text, step.Present))
                {
                    already.Add(label);
                    continue;
                }

                var lines = text.Replace("\r\n", "\n").Split('\n').ToList();
                string? error = step.Apply(lines);
                if (error is not null)
                    return Fail($"{layout.Relative(step.Path)} içinde {error} bulunamadı; dosya elle değişmiş olabilir. Kaydı elle ekleyin ya da dosyayı düzeltin.");

                edits.Add(new PlannedEdit(step.Path, string.Join(newline, lines), step.Description, bom));
            }

            return new RegistrationOutcome(edits, already, null);

            RegistrationOutcome Fail(string message) => new([], [], message);
        }

        // Deseni sağlayan son satırın altına, onun girintisiyle yeni satır ekler. Bulamazsa hata metni döner.
        private static string? InsertAfterLast(List<string> lines, string pattern, Func<string, string> line, string anchorName)
        {
            int index = lines.FindLastIndex(l => Regex.IsMatch(l, pattern));
            if (index < 0)
                return anchorName;
            lines.Insert(index + 1, line(IndentOf(lines[index])));
            return null;
        }

        // OperationClaimSeeds.All dizisinin kapanışından ("];") önce 6 claim, dosyanın başına da Constants using'i.
        private static string? AddSeeds(List<string> lines, string e, Func<Guid> newGuid)
        {
            int start = lines.FindIndex(l => Regex.IsMatch(l, @"OperationClaim\[\]\s+All\s*="));
            if (start < 0)
                return "'OperationClaim[] All =' dizisi";
            int close = lines.FindIndex(start, l => l.Trim() == "];");
            if (close < 0)
                return "'All' dizisinin kapanışı ('];')";

            int lastItem = lines.FindLastIndex(close, close - start, l => l.TrimStart().StartsWith("new(", StringComparison.Ordinal));
            string indent = lastItem >= 0 ? IndentOf(lines[lastItem]) : IndentOf(lines[close]) + "    ";

            var seeds = new List<string> { string.Empty };
            seeds.AddRange(ClaimActions.Select(action =>
                $"{indent}new() {{ Id = Guid.Parse(\"{newGuid()}\"), Name = {e}OperationClaims.{action} }},"));
            lines.InsertRange(close, seeds);

            AddUsing(lines, $"ECommerce.Application.Features.{e}.Constants");
            return null;
        }

        // Using'i ECommerce.Application.Features.* using'leri arasına alfabetik yerine koyar.
        private static void AddUsing(List<string> lines, string ns)
        {
            string directive = $"using {ns};";
            if (lines.Any(l => l.Trim() == directive))
                return;

            var usingIndexes = lines.Select((l, i) => (l, i)).Where(x => x.l.StartsWith("using ", StringComparison.Ordinal)).Select(x => x.i).ToList();
            var featureUsings = usingIndexes.Where(i => lines[i].StartsWith("using ECommerce.Application.Features.", StringComparison.Ordinal)).ToList();

            int position = featureUsings
                .Where(i => string.CompareOrdinal(lines[i], directive) > 0)
                .Select(i => (int?)i)
                .FirstOrDefault()
                ?? (featureUsings.Count > 0 ? featureUsings[^1] + 1 : usingIndexes.Count > 0 ? usingIndexes[^1] + 1 : 0);
            lines.Insert(position, directive);
        }

        private static string IndentOf(string line) => line[..(line.Length - line.TrimStart().Length)];
    }
}
