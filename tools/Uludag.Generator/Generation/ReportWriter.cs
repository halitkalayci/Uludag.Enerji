using System.Text.RegularExpressions;
using Uludag.Generator.Analysis;
using Uludag.Generator.Workspace;

namespace Uludag.Generator.Generation
{
    // Düz metin çıktı (spec R-7): ne oluştu/değişti, ne atlandı, sonra ne yapılmalı. AI ajanı bu çıktıyı okur.
    internal static class ReportWriter
    {
        public static void WriteConflicts(GenerationPlan plan, RepoLayout layout, bool dryRun, TextWriter error)
        {
            error.WriteLine($"Çakışma: aşağıdaki dosyalar zaten var; hiçbir dosya yazılmadı ya da değiştirilmedi.");
            foreach (PlannedFile file in plan.Conflicts)
                error.WriteLine($"  = {layout.Relative(file.Path)}");
            error.WriteLine(dryRun
                ? "Üzerine yazmak için --force kullanın."
                : "Üzerine yazmak için --force, tam önizleme için --dry-run kullanın.");
            // Çıktıyı okuyan bir AI ajanı elle yazılmış dosyaları (ör. Auth repository'leri) ezmesin (review L-2, B-24).
            error.WriteLine("Uyarı: bu dosyalar elle yazılmış olabilir (ör. var olan bir feature ya da Auth repository'leri). --force yalnızca");
            error.WriteLine("uludag-gen'in ürettiği dosyaların üzerine yazmak içindir; var olan feature yeniden üretilmez (docs/conventions.md, Code generation).");
        }

        public static void Write(GenerationPlan plan, RepoLayout layout, bool dryRun, bool force, TextWriter output)
        {
            CrudModel m = plan.Model;
            string verbNew = dryRun ? "Oluşturulacak" : "Oluşturuldu";
            // Çakışan dosyalar yalnızca --force ile üzerine yazılır; yoksa (dry-run) yazılmayacakları belirtilir.
            string verbOverwrite = !force ? "Üzerine yazılacak (yalnızca --force ile)" : dryRun ? "Üzerine yazılacak" : "Üzerine yazıldı";
            string verbEdit = dryRun ? "Değiştirilecek" : "Değiştirildi";

            output.WriteLine($"uludag-gen crud {m.Entity} (çoğul: {m.Plural}){(dryRun ? " — DRY RUN: hiçbir dosyaya dokunulmadı" : string.Empty)}");
            output.WriteLine($"Kök: {layout.Root}");
            // Çakışmalı dry-run: stdout'u tek başına okuyan ajan da hiçbir şey yazılmayacağını bilsin (review round 2, L-A).
            if (!force && plan.Conflicts.Count > 0)
                output.WriteLine("ÇAKIŞMA VAR: --force olmadan hiçbir dosya yazılmaz ya da değiştirilmez; aşağıdaki plan --force ile çalıştırılırsa yapılacaklardır.");

            var created = plan.Files.Where(f => !f.Exists).ToList();
            var overwritten = plan.Files.Where(f => f.Exists).ToList();
            WriteSection(output, $"{verbNew} ({created.Count}):", created.Select(f => $"+ {layout.Relative(f.Path)}"));
            WriteSection(output, $"{verbOverwrite} ({overwritten.Count}):", overwritten.Select(f => $"! {layout.Relative(f.Path)}"));
            WriteSection(output, $"{verbEdit} ({plan.Edits.Count}):", plan.Edits.Select(e => $"~ {layout.Relative(e.Path)} — {e.Description}"));
            WriteSection(output, "Zaten kayıtlı (atlandı):", plan.AlreadyRegistered.Select(r => $"= {r}"));
            WriteSection(output, "Atlanan özellikler (CRUD'a girmez):", plan.SkippedProperties.Select(Describe));
            WriteSection(output, "Uyarılar:", plan.IdentityBoundProperties.Select(p =>
                $"! {p.Name} — kimliğe bağlı alan: Create/Update komutlarına girmedi, yanıtta var. Sunucuda (oturumdan) atanmalı; yoksa boş kalır (docs/conventions.md, Data rules)."));

            string features = $"ECommerce/src/ECommerce.Application/Features/{m.Entity}";
            output.WriteLine();
            output.WriteLine("Sonraki adımlar:");
            output.WriteLine($"  1. ZORUNLU — migration: yeni tablo ve operation claim seed'leri için migration ekleyin. Migration olmadan tablo");
            output.WriteLine($"     oluşmaz ve EF Core 9 bekleyen model değişikliğinde Migrate'i durdurur:");
            output.WriteLine($"     dotnet ef migrations add add_{SnakeCase(m.Entity)} -p ECommerce/src/ECommerce.Persistence -s ECommerce/src/ECommerce.WebAPI");
            output.WriteLine($"  2. Entity'ye özgü iş kuralları (ör. ad benzersizliği): {features}/Rules/{m.Entity}BusinessRules.cs —");
            output.WriteLine($"     handler'dan çağırın; handler'da if/else ve mapping yok (F-7).");
            output.WriteLine($"  3. Kimliğe bağlı alanlar (kullanıcı, tarih, fiyat, toplam) istemciden alınmaz: komut alanlarını gözden geçirin.");
            output.WriteLine($"  4. Alan uzunlukları, indeksler, ilişkiler: ECommerce/src/ECommerce.Persistence/Configurations/{m.Entity}Configuration.cs");
            output.WriteLine($"  5. Mesaj metinleri: {features}/Locales/{m.Entity}Locale.{{tr,en}}.yaml");
        }

        // Mevcut migration adlarıyla aynı biçim: OrderItem -> order_item (ör. add_order_user).
        // Kısaltmalar bölünmez: OTPCode -> otp_code (review round 2, I-A).
        private static string SnakeCase(string name) =>
            Regex.Replace(name, "(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", "_").ToLowerInvariant();

        private static string Describe(PropertyModel p) => p.Kind switch
        {
            PropertyKind.Navigation => $"- {p.Name} ({p.SourceType}) — navigation özelliği",
            PropertyKind.Collection => $"- {p.Name} ({p.SourceType}) — koleksiyon",
            _ => $"- {p.Name} ({p.SourceType}) — tanınmayan tip; gerekiyorsa elle ekleyin"
        };

        private static void WriteSection(TextWriter output, string title, IEnumerable<string> lines)
        {
            var list = lines.ToList();
            if (list.Count == 0)
                return;
            output.WriteLine();
            output.WriteLine(title);
            foreach (string line in list)
                output.WriteLine($"  {line}");
        }
    }
}
