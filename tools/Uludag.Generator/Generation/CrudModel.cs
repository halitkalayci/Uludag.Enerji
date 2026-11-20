using Microsoft.CodeAnalysis.CSharp;
using Uludag.Generator.Analysis;

namespace Uludag.Generator.Generation
{
    // Bir alanın komut/yanıt modelindeki karşılığı. Money iki alana (Amount + Currency) açılır.
    internal enum FieldPart
    {
        Whole,
        MoneyAmount,
        MoneyCurrency
    }

    internal sealed record FieldSpec(string Name, string Type, PropertyModel Source, FieldPart Part)
    {
        // Nullable kapalı referans tipler için başlatıcı (ProductResponse ile aynı: "= null!").
        public string Declaration => Type == "string" ? $"public {Type} {Name} {{ get; set; }} = null!;" : $"public {Type} {Name} {{ get; set; }}";
    }

    // Şablonların ihtiyaç duyduğu her şey: adlar, alanlar, using'ler. Şablonlar karar vermez, yalnızca yazar.
    internal sealed class CrudModel
    {
        public string Entity { get; }
        public string Plural { get; }
        public string Var { get; }          // tekil değişken: category (C# anahtar kelimesiyse @event)
        public string PluralVar { get; }    // çoğul değişken: categories
        public string Lambda { get; }       // lambda parametresi: c
        public string Camel { get; }        // alan adı öneki: categoryRepository

        public IReadOnlyList<FieldSpec> WriteFields { get; }   // Create/Update komutu
        public IReadOnlyList<FieldSpec> ReadFields { get; }    // Response (Id hariç)
        public IReadOnlyList<PropertyModel> MoneyProperties { get; }
        public IReadOnlyList<string> EnumNamespaces { get; }  // komut/yanıt dosyalarına using

        public CrudModel(EntityModel entity, string plural, IReadOnlyDictionary<string, string> enums)
        {
            Entity = entity.Name;
            Plural = plural;
            Camel = char.ToLowerInvariant(Entity[0]) + Entity[1..];
            Var = Escape(Camel);
            PluralVar = Escape(char.ToLowerInvariant(Plural[0]) + Plural[1..]);
            Lambda = char.ToLowerInvariant(Entity[0]).ToString();

            MoneyProperties = entity.Properties.Where(p => p.Kind == PropertyKind.Money).ToList();

            // Tek Money → Amount/Currency (Product şekli). Birden fazla Money ya da aynı adlı başka bir
            // özellik varsa (ör. OrderItem.Amount) → <Özellik>Amount/<Özellik>Currency.
            var otherNames = entity.Properties.Where(p => p.Kind != PropertyKind.Money).Select(p => p.Name).ToHashSet();
            bool prefixMoney = MoneyProperties.Count > 1 || otherNames.Contains("Amount") || otherNames.Contains("Currency");

            ReadFields = entity.Properties.Where(p => p.IsReadable).SelectMany(p => Expand(p, prefixMoney)).ToList();
            WriteFields = ReadFields.Where(f => f.Source.IsWritable).ToList();

            EnumNamespaces = entity.Properties
                .Where(p => p.IsReadable && enums.ContainsKey(p.TypeName))
                .Select(p => enums[p.TypeName])
                .Where(ns => ns.Length > 0)
                .Distinct()
                .Order(StringComparer.Ordinal)
                .ToList();
        }

        private static IEnumerable<FieldSpec> Expand(PropertyModel p, bool prefixMoney)
        {
            if (p.Kind != PropertyKind.Money)
            {
                yield return new FieldSpec(p.Name, p.SourceType, p, FieldPart.Whole);
                yield break;
            }
            string prefix = prefixMoney ? p.Name : string.Empty;
            yield return new FieldSpec(prefix + "Amount", "decimal", p, FieldPart.MoneyAmount);
            yield return new FieldSpec(prefix + "Currency", "string", p, FieldPart.MoneyCurrency);
        }

        // Ayrılmış anahtar kelimeyle çakışan değişken adları @ ile kaçırılır (Event -> @event).
        private static string Escape(string identifier) =>
            SyntaxFacts.GetKeywordKind(identifier) != SyntaxKind.None ? "@" + identifier : identifier;
    }
}
