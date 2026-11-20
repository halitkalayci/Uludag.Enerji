namespace Uludag.Generator.Naming
{
    // Basit İngilizce çoğul kuralları (spec R-2, G-2). Düzensiz çoğullar için --plural kullanılır.
    //   consonant + y -> ies  (Category -> Categories; Key -> Keys)
    //   s, x, z, ch, sh -> es (Address -> Addresses, Box -> Boxes, Branch -> Branches)
    //   diğerleri -> s        (Product -> Products)
    internal static class Pluralizer
    {
        public static string Pluralize(string singular)
        {
            if (singular.Length > 1 && singular.EndsWith('y') && !IsVowel(singular[^2]))
                return singular[..^1] + "ies";
            if (singular.EndsWith('s') || singular.EndsWith('x') || singular.EndsWith('z')
                || singular.EndsWith("ch", StringComparison.Ordinal) || singular.EndsWith("sh", StringComparison.Ordinal))
                return singular + "es";
            return singular + "s";
        }

        private static bool IsVowel(char c) => "aeiouAEIOU".Contains(c);
    }
}
