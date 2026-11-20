using Uludag.Generator.Analysis;

namespace Uludag.Generator.Generation
{
    // Oluşturulacak yeni dosya. Exists: hedefte zaten bir dosya var (çakışma; --force ile üzerine yazılır).
    internal sealed record PlannedFile(string Path, string Content, bool Exists);

    // Var olan bir kayıt dosyasına yapılacak ekleme (DbSet, DI, seed). Content: dosyanın yeni hali.
    internal sealed record PlannedEdit(string Path, string Content, string Description);

    // Bellekte hazırlanmış üretimin tamamı: önce bu hazırlanır, çakışma yoksa hepsi birden yazılır.
    internal sealed class GenerationPlan
    {
        public required CrudModel Model { get; init; }
        public required IReadOnlyList<PlannedFile> Files { get; init; }
        public required IReadOnlyList<PlannedEdit> Edits { get; init; }
        public required IReadOnlyList<string> AlreadyRegistered { get; init; }
        public required IReadOnlyList<PropertyModel> SkippedProperties { get; init; }
        public required IReadOnlyList<PropertyModel> IdentityBoundProperties { get; init; }

        public IReadOnlyList<PlannedFile> Conflicts => Files.Where(f => f.Exists).ToList();
    }

    internal sealed record PlanResult(GenerationPlan? Plan, string? Error)
    {
        public static PlanResult Ok(GenerationPlan plan) => new(plan, null);
        public static PlanResult Fail(string error) => new(null, error);
    }
}
