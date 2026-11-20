namespace Uludag.Generator.Templates
{
    // Şablon yardımcıları: girintili satır blokları ve using listeleri.
    internal static class TemplateText
    {
        public const string GeneratedNote = "uludag-gen ile üretildi (spec 0005); serbestçe düzenlenebilir.";

        // Her satırı verilen girintiyle yazar; boş liste boş metin döner.
        public static string Block(IEnumerable<string> lines, int indent)
        {
            string pad = new(' ', indent);
            return string.Join("\n", lines.Select(l => l.Length == 0 ? l : pad + l));
        }

        public static string Usings(IEnumerable<string> namespaces) =>
            string.Join("\n", namespaces.Distinct().Order(StringComparer.Ordinal).Select(ns => $"using {ns};"));

        // Satır sonlarını LF'ye çevirir, boşluk-yalnız satırları temizler, tek satır sonuyla bitirir.
        public static string Normalize(string text)
        {
            var lines = text.Replace("\r\n", "\n").Split('\n').Select(l => l.TrimEnd());
            var result = new List<string>();
            foreach (string line in lines)
            {
                // Boş blok yerleşiminden doğan art arda boş satırları teke indir.
                if (line.Length == 0 && result.Count > 0 && result[^1].Length == 0)
                    continue;
                // Kapanan süslü parantezden hemen önceki boş satırı kaldır.
                if (line.TrimStart().StartsWith('}') && result.Count > 0 && result[^1].Length == 0)
                    result.RemoveAt(result.Count - 1);
                result.Add(line);
            }
            return string.Join("\n", result).Trim('\n') + "\n";
        }

        // Nesne başlatıcı öğeleri: son öğede virgül olmaz.
        public static IEnumerable<string> InitializerItems(IEnumerable<string> assignments)
        {
            var list = assignments.ToList();
            return list.Select((a, i) => i < list.Count - 1 ? a + "," : a);
        }
    }
}
