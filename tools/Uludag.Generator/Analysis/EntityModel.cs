namespace Uludag.Generator.Analysis
{
    internal sealed record EntityModel(string Name, string Namespace, IReadOnlyList<PropertyModel> Properties)
    {
        public bool HasGuidId => Properties.Any(p => p.Kind == PropertyKind.Id);
    }

    // Domain'den okunan her şey: entity'ler, enum'lar (ad → namespace) ve value object adları.
    internal sealed record DomainModel(
        IReadOnlyList<EntityModel> Entities,
        IReadOnlyDictionary<string, string> Enums,
        IReadOnlySet<string> ValueObjects)
    {
        public EntityModel? FindEntity(string name) => Entities.FirstOrDefault(e => e.Name == name);
    }
}
