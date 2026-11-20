namespace Uludag.Generator.Analysis
{
    // Bir entity özelliğinin üretimdeki rolü (spec R-4).
    internal enum PropertyKind
    {
        Id,          // Guid Id: Create'e girmez, route'tan gelir
        Scalar,      // string, sayısal, bool, DateTime, Guid, enum (+ nullable): aynen taşınır
        Money,       // Money VO: Amount + Currency olarak düzleştirilir
        Navigation,  // entity tipli özellik: atlanır
        Collection,  // koleksiyon: atlanır
        Unknown      // tanınmayan tip: atlanır, uyarı
    }

    // TypeName: kaynak koddaki yazım, nullable işareti hariç (ör. "string", "ICollection<OrderItem>").
    internal sealed record PropertyModel(string Name, string TypeName, PropertyKind Kind, bool IsNullable)
    {
        // Kimliğe bağlı alan (docs/conventions.md, Data rules): istemci gövdesinden alınmaz (spec R-4).
        public bool IsIdentityBound => Kind == PropertyKind.Scalar && Name == "UserId";

        // Komut ve mapper'a giren alanlar.
        public bool IsWritable => (Kind is PropertyKind.Scalar or PropertyKind.Money) && !IsIdentityBound;

        // Yanıta giren alanlar (Id hariç; Id her yanıtta ayrıca vardır).
        public bool IsReadable => Kind is PropertyKind.Scalar or PropertyKind.Money;

        public bool IsSkipped => Kind is PropertyKind.Navigation or PropertyKind.Collection or PropertyKind.Unknown;

        public string SourceType => IsNullable ? TypeName + "?" : TypeName;
    }
}
