using System.Text;
using Uludag.Generator.Analysis;
using Uludag.Generator.Naming;
using Uludag.Generator.Templates;
using Uludag.Generator.Workspace;

namespace Uludag.Generator.Generation
{
    // CRUD dilimini üretir: önce her şey bellekte hazırlanır (Plan), sonra hepsi birden yazılır (Apply).
    // Çakışma kontrolü Plan'dan sonra, Apply'dan önce yapılır: hepsi ya da hiçbiri (spec R-6).
    internal static class CrudGenerator
    {
        private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);
        private static readonly UTF8Encoding Utf8WithBom = new(encoderShouldEmitUTF8Identifier: true);

        public static PlanResult Plan(RepoLayout layout, DomainModel domain, string entityName, string? plural)
        {
            EntityModel? entity = domain.FindEntity(entityName);
            if (entity is null)
            {
                if (domain.Enums.ContainsKey(entityName))
                    return PlanResult.Fail($"'{entityName}' bir enum; CRUD yalnızca entity sınıfları için üretilir.");
                if (domain.ValueObjects.Contains(entityName))
                    return PlanResult.Fail($"'{entityName}' bir value object; CRUD yalnızca entity sınıfları için üretilir.");
                string known = string.Join(", ", domain.Entities.Select(e => e.Name));
                return PlanResult.Fail($"'{entityName}' adlı entity '{layout.Relative(layout.EntitiesDir)}' altında yok. Mevcut entity'ler: {known}.");
            }
            if (!entity.HasGuidId)
                return PlanResult.Fail($"'{entityName}' entity'sinde 'Guid Id' özelliği yok; araç yalnızca Guid Id'li entity'leri destekler (docs/conventions.md, Data rules).");

            var model = new CrudModel(entity, plural ?? Pluralizer.Pluralize(entity.Name), domain.Enums);

            var files = NewFiles(layout, model)
                .Select(f => new PlannedFile(f.Path, TemplateText.Normalize(f.Content), File.Exists(f.Path)))
                .ToList();

            RegistrationOutcome registrations = RegistrationEditor.Plan(layout, model, Guid.NewGuid);
            if (registrations.Error is not null)
                return PlanResult.Fail(registrations.Error);

            return PlanResult.Ok(new GenerationPlan
            {
                Model = model,
                Files = files,
                Edits = registrations.Edits,
                AlreadyRegistered = registrations.AlreadyRegistered,
                SkippedProperties = entity.Properties.Where(p => p.IsSkipped).ToList(),
                IdentityBoundProperties = entity.Properties.Where(p => p.IsIdentityBound).ToList()
            });
        }

        public static void Apply(GenerationPlan plan)
        {
            foreach (PlannedFile file in plan.Files)
                Write(file.Path, file.Content, Utf8NoBom);
            foreach (PlannedEdit edit in plan.Edits)
                Write(edit.Path, edit.Content, edit.Bom ? Utf8WithBom : Utf8NoBom);
        }

        private static void Write(string path, string content, Encoding encoding)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content, encoding);
        }

        // Spec R-2: 16 dosya, Product/Order ile aynı yol ve ad düzeninde.
        private static IEnumerable<(string Path, string Content)> NewFiles(RepoLayout layout, CrudModel m)
        {
            string e = m.Entity;
            string feature = Path.Combine(layout.ApplicationDir, "Features", e);

            yield return (Path.Combine(feature, "Commands", "Create", $"Create{e}Command.cs"), ApplicationTemplates.CreateCommand(m));
            yield return (Path.Combine(feature, "Commands", "Update", $"Update{e}Command.cs"), ApplicationTemplates.UpdateCommand(m));
            yield return (Path.Combine(feature, "Commands", "Delete", $"Delete{e}Command.cs"), ApplicationTemplates.DeleteCommand(m));
            yield return (Path.Combine(feature, "Queries", "GetAll", $"GetAll{e}Query.cs"), ApplicationTemplates.GetAllQuery(m));
            yield return (Path.Combine(feature, "Queries", "GetById", $"GetById{e}Query.cs"), ApplicationTemplates.GetByIdQuery(m));
            yield return (Path.Combine(feature, "Queries", $"{e}Response.cs"), ApplicationTemplates.Response(m));
            yield return (Path.Combine(feature, "Rules", $"{e}BusinessRules.cs"), ApplicationTemplates.BusinessRules(m));
            yield return (Path.Combine(feature, "Mappers", $"{e}Mapper.cs"), ApplicationTemplates.Mapper(m));
            yield return (Path.Combine(feature, "Constants", $"{e}OperationClaims.cs"), ApplicationTemplates.OperationClaims(m));
            yield return (Path.Combine(feature, "Constants", $"{e}Constants.cs"), ApplicationTemplates.Constants(m));
            yield return (Path.Combine(feature, "Locales", $"{e}Locale.tr.yaml"), LocaleTemplates.Turkish(m));
            yield return (Path.Combine(feature, "Locales", $"{e}Locale.en.yaml"), LocaleTemplates.English(m));
            yield return (Path.Combine(layout.ApplicationDir, "Services", "Repositories", $"I{e}Repository.cs"), ApplicationTemplates.RepositoryInterface(m));
            yield return (Path.Combine(layout.PersistenceDir, "Repositories", $"{e}Repository.cs"), PersistenceTemplates.Repository(m));
            yield return (Path.Combine(layout.PersistenceDir, "Configurations", $"{e}Configuration.cs"), PersistenceTemplates.Configuration(m));
            yield return (Path.Combine(layout.WebApiDir, "Controllers", $"{m.Plural}Controller.cs"), WebApiTemplates.Controller(m));
        }
    }
}
