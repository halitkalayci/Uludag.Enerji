using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Uludag.Generator.Workspace;

namespace Uludag.Generator.Analysis
{
    // Domain'i kaynak koddan okur (Roslyn sözdizimi ağacı; derleme/semantik model yok).
    // Böylece çözüm derlenmiyorken de çalışır (spec R-1, AC-1).
    internal static class EntityReader
    {
        private static readonly HashSet<string> ScalarTypeNames =
        [
            "string", "bool", "byte", "sbyte", "short", "ushort", "int", "uint", "long", "ulong",
            "float", "double", "decimal", "char",
            "Guid", "DateTime", "DateTimeOffset", "DateOnly", "TimeOnly", "TimeSpan",
            "String", "Boolean", "Int16", "Int32", "Int64", "Decimal", "Double", "Single"
        ];

        private static readonly HashSet<string> CollectionTypeNames =
        [
            "ICollection", "IList", "List", "IEnumerable", "IReadOnlyCollection", "IReadOnlyList",
            "HashSet", "ISet", "Collection"
        ];

        public const string MoneyTypeName = "Money";

        public static DomainModel Read(RepoLayout layout)
        {
            var entityTrees = ParseAll(layout.EntitiesDir);
            var valueObjectTrees = ParseAll(layout.ValueObjectsDir);
            var allRoots = entityTrees.Concat(valueObjectTrees).ToList();

            // Enum'lar Domain'in her yerinden (Entities ve ValueObjects) toplanır.
            var enums = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var root in allRoots)
                foreach (var e in root.DescendantNodes().OfType<EnumDeclarationSyntax>())
                    enums[e.Identifier.Text] = NamespaceOf(e);

            var valueObjects = new HashSet<string>(StringComparer.Ordinal);
            foreach (var root in valueObjectTrees)
                foreach (var t in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
                    valueObjects.Add(t.Identifier.Text);

            // Entity adayları: Entities altındaki somut sınıflar (abstract/static hariç).
            var classes = entityTrees
                .SelectMany(r => r.DescendantNodes().OfType<ClassDeclarationSyntax>())
                .Where(c => !c.Modifiers.Any(SyntaxKind.AbstractKeyword) && !c.Modifiers.Any(SyntaxKind.StaticKeyword))
                .Where(c => c.Parent is BaseNamespaceDeclarationSyntax or CompilationUnitSyntax) // iç içe sınıf değil
                .ToList();
            var entityNames = new HashSet<string>(classes.Select(c => c.Identifier.Text), StringComparer.Ordinal);

            var entities = classes
                .Select(c => new EntityModel(
                    c.Identifier.Text,
                    NamespaceOf(c),
                    c.Members.OfType<PropertyDeclarationSyntax>()
                        .Where(IsPublicInstanceReadWrite)
                        .Select(p => Classify(p, entityNames, enums))
                        .ToList()))
                .GroupBy(e => e.Name) // partial sınıflar: ilk tanım yeterli
                .Select(g => g.First())
                .OrderBy(e => e.Name, StringComparer.Ordinal)
                .ToList();

            return new DomainModel(entities, enums, valueObjects);
        }

        private static List<SyntaxNode> ParseAll(string dir)
        {
            if (!Directory.Exists(dir))
                return [];
            return Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories)
                .Order(StringComparer.Ordinal)
                .Select(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f), path: f).GetRoot())
                .ToList();
        }

        private static bool IsPublicInstanceReadWrite(PropertyDeclarationSyntax p)
        {
            if (!p.Modifiers.Any(SyntaxKind.PublicKeyword) || p.Modifiers.Any(SyntaxKind.StaticKeyword))
                return false;
            var accessors = p.AccessorList?.Accessors;
            if (accessors is null)
                return false; // expression-bodied (=>) hesaplanan özellik
            bool hasGet = accessors.Value.Any(a => a.IsKind(SyntaxKind.GetAccessorDeclaration));
            bool hasSet = accessors.Value.Any(a =>
                (a.IsKind(SyntaxKind.SetAccessorDeclaration) || a.IsKind(SyntaxKind.InitAccessorDeclaration))
                && !a.Modifiers.Any(SyntaxKind.PrivateKeyword) && !a.Modifiers.Any(SyntaxKind.ProtectedKeyword));
            return hasGet && hasSet;
        }

        private static PropertyModel Classify(PropertyDeclarationSyntax p, IReadOnlySet<string> entityNames, IReadOnlyDictionary<string, string> enums)
        {
            string name = p.Identifier.Text;
            TypeSyntax type = p.Type;
            bool nullable = false;
            if (type is NullableTypeSyntax n)
            {
                nullable = true;
                type = n.ElementType;
            }
            string typeName = type.ToString();

            GenericNameSyntax? generic = type switch
            {
                GenericNameSyntax g => g,
                QualifiedNameSyntax { Right: GenericNameSyntax g } => g,
                _ => null
            };
            if (generic is not null)
                return new PropertyModel(name, typeName,
                    CollectionTypeNames.Contains(generic.Identifier.Text) ? PropertyKind.Collection : PropertyKind.Unknown, nullable);

            if (type is ArrayTypeSyntax)
                return new PropertyModel(name, typeName, PropertyKind.Unknown, nullable);

            string simple = type switch
            {
                PredefinedTypeSyntax pre => pre.Keyword.Text,
                IdentifierNameSyntax id => id.Identifier.Text,
                QualifiedNameSyntax q => q.Right.Identifier.Text,
                AliasQualifiedNameSyntax a => a.Name.Identifier.Text,
                _ => typeName
            };

            PropertyKind kind = simple switch
            {
                "Guid" when name == "Id" && !nullable => PropertyKind.Id,
                _ when ScalarTypeNames.Contains(simple) || enums.ContainsKey(simple) => PropertyKind.Scalar,
                MoneyTypeName when !nullable => PropertyKind.Money,
                _ when entityNames.Contains(simple) => PropertyKind.Navigation,
                _ => PropertyKind.Unknown
            };
            return new PropertyModel(name, typeName, kind, nullable);
        }

        private static string NamespaceOf(SyntaxNode node)
        {
            var parts = node.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().Select(n => n.Name.ToString()).Reverse();
            return string.Join(".", parts);
        }
    }
}
