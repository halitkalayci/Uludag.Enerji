using Uludag.Generator.Generation;

namespace Uludag.Generator.Templates
{
    // Mesaj metinleri (docs/conventions.md: Locales/<Feature>Locale.{tr,en}.yaml). Kalıp: OrderLocale.
    internal static class LocaleTemplates
    {
        public static string Turkish(CrudModel m) => $"{m.Entity}NotFound: \"{m.Entity} kaydı bulunamadı.\"\n";

        public static string English(CrudModel m) => $"{m.Entity}NotFound: \"{m.Entity} not found.\"\n";
    }
}
