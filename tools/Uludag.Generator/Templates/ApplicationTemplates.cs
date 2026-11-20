using Uludag.Generator.Generation;
using static Uludag.Generator.Templates.TemplateText;

namespace Uludag.Generator.Templates
{
    // ECommerce.Application şablonları. Kalıp: Features/Product (komut/sorgu/handler) ve Features/Order
    // (Constants + Locales mesaj anahtarları, docs/conventions.md). Handler'larda if/else ve mapping yok (F-7):
    // kontroller Rules'ta, dönüşümler Mapper'da.
    internal static class ApplicationTemplates
    {
        private static string Ns(CrudModel m) => $"ECommerce.Application.Features.{m.Entity}";

        private static string FieldBlock(IEnumerable<FieldSpec> fields) => Block(fields.Select(f => f.Declaration), 8);

        public static string CreateCommand(CrudModel m) => $$"""
            {{Usings(["ECommerce.Application.Services.Repositories", $"{Ns(m)}.Constants", $"{Ns(m)}.Mappers", "Uludag.Core.Mediation", .. m.EnumNamespaces])}}

            namespace {{Ns(m)}}.Commands.Create
            {
                // {{GeneratedNote}}
                // Entity'ye özgü iş kuralları (ör. ad benzersizliği) {{m.Entity}}BusinessRules'a eklenir ve handler'dan çağrılır.
                public class Create{{m.Entity}}Command : IRequest, ISecuredRequest
                {
                    // Yetki: {{m.Entity}}.Admin, {{m.Entity}}.Write veya {{m.Entity}}.Create claim'lerinden biri yeterli (global Admin her zaman geçer).
                    // Açık (explicit) implementasyon: public property olmaz, JSON gövdesine / Swagger'a girmez.
                    string[] ISecuredRequest.Roles => [{{m.Entity}}OperationClaims.Admin, {{m.Entity}}OperationClaims.Write, {{m.Entity}}OperationClaims.Create];

            {{FieldBlock(m.WriteFields)}}

                    public class Create{{m.Entity}}CommandHandler : IRequestHandler<Create{{m.Entity}}Command>
                    {
                        private readonly I{{m.Entity}}Repository {{m.Camel}}Repository;
                        public Create{{m.Entity}}CommandHandler(I{{m.Entity}}Repository {{m.Camel}}Repository)
                        {
                            this.{{m.Camel}}Repository = {{m.Camel}}Repository;
                        }

                        public async Task Handle(Create{{m.Entity}}Command request, CancellationToken cancellationToken)
                        {
                            Domain.Entities.{{m.Entity}} {{m.Var}} = {{m.Entity}}Mapper.MapCreateCommandTo{{m.Entity}}(request);

                            await {{m.Camel}}Repository.AddAsync({{m.Var}}, cancellationToken);
                        }
                    }
                }
            }
            """;

        public static string UpdateCommand(CrudModel m) => $$"""
            {{Usings(["ECommerce.Application.Services.Repositories", $"{Ns(m)}.Constants", $"{Ns(m)}.Mappers", $"{Ns(m)}.Rules", "Uludag.Core.Mediation", .. m.EnumNamespaces])}}

            namespace {{Ns(m)}}.Commands.Update
            {
                // {{GeneratedNote}}
                // Tam değiştirme (PUT): tüm alanlar gönderilir. Kayıt bulunamazsa NotFoundException fırlar (404).
                public class Update{{m.Entity}}Command : IRequest, ISecuredRequest
                {
                    // Yetki: {{m.Entity}}.Admin, {{m.Entity}}.Write veya {{m.Entity}}.Update claim'lerinden biri yeterli (global Admin her zaman geçer).
                    // Açık (explicit) implementasyon: public property olmaz, JSON gövdesine / Swagger'a girmez.
                    string[] ISecuredRequest.Roles => [{{m.Entity}}OperationClaims.Admin, {{m.Entity}}OperationClaims.Write, {{m.Entity}}OperationClaims.Update];

                    public Guid Id { get; set; }
            {{FieldBlock(m.WriteFields)}}

                    public class Update{{m.Entity}}CommandHandler : IRequestHandler<Update{{m.Entity}}Command>
                    {
                        private readonly I{{m.Entity}}Repository {{m.Camel}}Repository;
                        private readonly {{m.Entity}}BusinessRules {{m.Camel}}BusinessRules;
                        public Update{{m.Entity}}CommandHandler(I{{m.Entity}}Repository {{m.Camel}}Repository, {{m.Entity}}BusinessRules {{m.Camel}}BusinessRules)
                        {
                            this.{{m.Camel}}Repository = {{m.Camel}}Repository;
                            this.{{m.Camel}}BusinessRules = {{m.Camel}}BusinessRules;
                        }

                        public async Task Handle(Update{{m.Entity}}Command request, CancellationToken cancellationToken)
                        {
                            Domain.Entities.{{m.Entity}}? {{m.Var}} = await {{m.Camel}}Repository.GetByIdAsync(request.Id, cancellationToken);
                            {{m.Camel}}BusinessRules.{{m.Entity}}ShouldExistWhenSelected({{m.Var}});

                            {{m.Entity}}Mapper.MapUpdateCommandTo{{m.Entity}}(request, {{m.Var}});

                            await {{m.Camel}}Repository.UpdateAsync({{m.Var}}, cancellationToken);
                        }
                    }
                }
            }
            """;

        public static string DeleteCommand(CrudModel m) => $$"""
            {{Usings(["ECommerce.Application.Services.Repositories", $"{Ns(m)}.Constants", $"{Ns(m)}.Rules", "Uludag.Core.Mediation"])}}

            namespace {{Ns(m)}}.Commands.Delete
            {
                // {{GeneratedNote}}
                // Kayıt bulunamazsa NotFoundException fırlar (404).
                public class Delete{{m.Entity}}Command : IRequest, ISecuredRequest
                {
                    // Yetki: {{m.Entity}}.Admin, {{m.Entity}}.Write veya {{m.Entity}}.Delete claim'lerinden biri yeterli (global Admin her zaman geçer).
                    // Açık (explicit) implementasyon: public property olmaz, JSON gövdesine / Swagger'a girmez.
                    string[] ISecuredRequest.Roles => [{{m.Entity}}OperationClaims.Admin, {{m.Entity}}OperationClaims.Write, {{m.Entity}}OperationClaims.Delete];

                    public Guid Id { get; set; }

                    public class Delete{{m.Entity}}CommandHandler : IRequestHandler<Delete{{m.Entity}}Command>
                    {
                        private readonly I{{m.Entity}}Repository {{m.Camel}}Repository;
                        private readonly {{m.Entity}}BusinessRules {{m.Camel}}BusinessRules;
                        public Delete{{m.Entity}}CommandHandler(I{{m.Entity}}Repository {{m.Camel}}Repository, {{m.Entity}}BusinessRules {{m.Camel}}BusinessRules)
                        {
                            this.{{m.Camel}}Repository = {{m.Camel}}Repository;
                            this.{{m.Camel}}BusinessRules = {{m.Camel}}BusinessRules;
                        }

                        public async Task Handle(Delete{{m.Entity}}Command request, CancellationToken cancellationToken)
                        {
                            Domain.Entities.{{m.Entity}}? {{m.Var}} = await {{m.Camel}}Repository.GetByIdAsync(request.Id, cancellationToken);
                            {{m.Camel}}BusinessRules.{{m.Entity}}ShouldExistWhenSelected({{m.Var}});

                            await {{m.Camel}}Repository.DeleteAsync({{m.Var}}, cancellationToken);
                        }
                    }
                }
            }
            """;

        public static string GetAllQuery(CrudModel m) => $$"""
            {{Usings(["ECommerce.Application.Services.Repositories", $"{Ns(m)}.Constants", $"{Ns(m)}.Mappers", "Uludag.Core.Mediation"])}}

            namespace {{Ns(m)}}.Queries.GetAll
            {
                // {{GeneratedNote}}
                // Gövdesi boş sorgu: tüm kayıtları ister.
                public class GetAll{{m.Entity}}Query : IRequest<List<{{m.Entity}}Response>>, ISecuredRequest
                {
                    // Yetki: {{m.Entity}}.Admin veya {{m.Entity}}.Read claim'lerinden biri yeterli (global Admin her zaman geçer).
                    // Açık (explicit) implementasyon: public property olmaz, JSON gövdesine / Swagger'a girmez.
                    string[] ISecuredRequest.Roles => [{{m.Entity}}OperationClaims.Admin, {{m.Entity}}OperationClaims.Read];

                    public class GetAll{{m.Entity}}QueryHandler : IRequestHandler<GetAll{{m.Entity}}Query, List<{{m.Entity}}Response>>
                    {
                        private readonly I{{m.Entity}}Repository {{m.Camel}}Repository;
                        public GetAll{{m.Entity}}QueryHandler(I{{m.Entity}}Repository {{m.Camel}}Repository)
                        {
                            this.{{m.Camel}}Repository = {{m.Camel}}Repository;
                        }

                        public async Task<List<{{m.Entity}}Response>> Handle(GetAll{{m.Entity}}Query request, CancellationToken cancellationToken)
                        {
                            List<Domain.Entities.{{m.Entity}}> {{m.PluralVar}} = await {{m.Camel}}Repository.GetAllAsync(cancellationToken);

                            return {{m.Entity}}Mapper.Map{{m.Plural}}ToResponses({{m.PluralVar}});
                        }
                    }
                }
            }
            """;

        public static string GetByIdQuery(CrudModel m) => $$"""
            {{Usings(["ECommerce.Application.Services.Repositories", $"{Ns(m)}.Constants", $"{Ns(m)}.Mappers", $"{Ns(m)}.Rules", "Uludag.Core.Mediation"])}}

            namespace {{Ns(m)}}.Queries.GetById
            {
                // {{GeneratedNote}}
                // Kayıt yoksa NotFoundException fırlar (404); null dönmez.
                public class GetById{{m.Entity}}Query : IRequest<{{m.Entity}}Response>, ISecuredRequest
                {
                    // Yetki: {{m.Entity}}.Admin veya {{m.Entity}}.Read claim'lerinden biri yeterli (global Admin her zaman geçer).
                    // Açık (explicit) implementasyon: public property olmaz, JSON gövdesine / Swagger'a girmez.
                    string[] ISecuredRequest.Roles => [{{m.Entity}}OperationClaims.Admin, {{m.Entity}}OperationClaims.Read];

                    public Guid Id { get; set; }

                    public class GetById{{m.Entity}}QueryHandler : IRequestHandler<GetById{{m.Entity}}Query, {{m.Entity}}Response>
                    {
                        private readonly I{{m.Entity}}Repository {{m.Camel}}Repository;
                        private readonly {{m.Entity}}BusinessRules {{m.Camel}}BusinessRules;
                        public GetById{{m.Entity}}QueryHandler(I{{m.Entity}}Repository {{m.Camel}}Repository, {{m.Entity}}BusinessRules {{m.Camel}}BusinessRules)
                        {
                            this.{{m.Camel}}Repository = {{m.Camel}}Repository;
                            this.{{m.Camel}}BusinessRules = {{m.Camel}}BusinessRules;
                        }

                        public async Task<{{m.Entity}}Response> Handle(GetById{{m.Entity}}Query request, CancellationToken cancellationToken)
                        {
                            Domain.Entities.{{m.Entity}}? {{m.Var}} = await {{m.Camel}}Repository.GetByIdAsync(request.Id, cancellationToken);
                            {{m.Camel}}BusinessRules.{{m.Entity}}ShouldExistWhenSelected({{m.Var}});

                            return {{m.Entity}}Mapper.Map{{m.Entity}}ToResponse({{m.Var}});
                        }
                    }
                }
            }
            """;

        public static string Response(CrudModel m) => $$"""
            {{Usings(m.EnumNamespaces)}}

            namespace {{Ns(m)}}.Queries
            {
                // {{GeneratedNote}}
                // Sorguların dışarıya döndüğü model. Entity doğrudan dışarı açılmaz; Money düz alanlara açılır.
                public class {{m.Entity}}Response
                {
                    public Guid Id { get; set; }
            {{FieldBlock(m.ReadFields)}}
                }
            }
            """;

        public static string BusinessRules(CrudModel m) => $$"""
            using ECommerce.Application.Exceptions;
            using {{Ns(m)}}.Constants;
            using System.Diagnostics.CodeAnalysis;

            namespace {{Ns(m)}}.Rules
            {
                // {{GeneratedNote}}
                // {{m.Entity}} ile ilgili tüm iş kuralları burada yazılır; handler'lar if-else yapmaz, bu metotları çağırır (F-7).
                // Kural ihlal edilirse metot istisna fırlatır, edilmezse sessizce döner.
                // Otomatik DI kaydı: BaseBusinessRules'tan türediği için AddSubClassesOfType ile Scoped eklenir.
                public class {{m.Entity}}BusinessRules : BaseBusinessRules
                {
                    // [NotNull]: Metot hata fırlatmadan dönerse derleyici kaydın artık null olmadığını bilir.
                    public void {{m.Entity}}ShouldExistWhenSelected([NotNull] Domain.Entities.{{m.Entity}}? {{m.Var}})
                    {
                        if ({{m.Var}} is null)
                            throw new NotFoundException({{m.Entity}}Constants.{{m.Entity}}NotFound);
                    }
                }
            }
            """;

        public static string Mapper(CrudModel m)
        {
            string e = m.Entity;
            var create = m.WriteFields.Select(f => f.Part switch
            {
                FieldPart.Whole => $"{f.Name} = command.{f.Name}",
                FieldPart.MoneyAmount => $"{f.Source.Name} = new Domain.ValueObjects.Money(command.{f.Name}, command.{CurrencyOf(m, f)})",
                _ => null
            }).OfType<string>();
            var update = m.WriteFields.Select(f => f.Part switch
            {
                FieldPart.Whole => $"{m.Var}.{f.Name} = command.{f.Name};",
                FieldPart.MoneyAmount => $"{m.Var}.{f.Source.Name} = new Domain.ValueObjects.Money(command.{f.Name}, command.{CurrencyOf(m, f)});",
                _ => null
            }).OfType<string>();
            var response = m.ReadFields.Select(f => f.Part switch
            {
                FieldPart.Whole => $"{f.Name} = {m.Var}.{f.Name}",
                FieldPart.MoneyAmount => $"{f.Name} = {m.Var}.{f.Source.Name}.Amount",
                _ => $"{f.Name} = {m.Var}.{f.Source.Name}.Currency"
            });

            return $$"""
                using {{Ns(m)}}.Commands.Create;
                using {{Ns(m)}}.Commands.Update;
                using {{Ns(m)}}.Queries;

                namespace {{Ns(m)}}.Mappers
                {
                    // {{GeneratedNote}}
                    // {{e}} feature'ının tüm mapping'leri burada yazılır; handler'lar mapping yapmaz, bu metotları çağırır (F-7).
                    public static class {{e}}Mapper
                    {
                        public static Domain.Entities.{{e}} MapCreateCommandTo{{e}}(Create{{e}}Command command)
                        {
                            return new Domain.Entities.{{e}}
                            {
                {{Block(InitializerItems(create), 16)}}
                            };
                        }

                        // Var olan (DB'den gelen, EF tarafından takip edilen) entity'nin üzerine yazar; yeni instance oluşturmaz.
                        public static void MapUpdateCommandTo{{e}}(Update{{e}}Command command, Domain.Entities.{{e}} {{m.Var}})
                        {
                {{Block(update, 12)}}
                        }

                        public static {{e}}Response Map{{e}}ToResponse(Domain.Entities.{{e}} {{m.Var}})
                        {
                            return new {{e}}Response
                            {
                {{Block(InitializerItems(response.Prepend($"Id = {m.Var}.Id")), 16)}}
                            };
                        }

                        public static List<{{e}}Response> Map{{m.Plural}}ToResponses(List<Domain.Entities.{{e}}> {{m.PluralVar}})
                        {
                            return {{m.PluralVar}}.Select(Map{{e}}ToResponse).ToList();
                        }
                    }
                }
                """;
        }

        // Money'nin Amount alanına eşlik eden Currency alanı.
        private static string CurrencyOf(CrudModel m, FieldSpec amount) =>
            m.WriteFields.Single(f => f.Part == FieldPart.MoneyCurrency && f.Source == amount.Source).Name;

        public static string OperationClaims(CrudModel m) => $$"""
            namespace {{Ns(m)}}.Constants
            {
                // {{GeneratedNote}}
                // {{m.Entity}} işlemlerinin operation claim adları. Karşılaştırma metinseldir (birebir, büyük/küçük harf duyarlı).
                // Varsayılan yapı:
                //   Create / Update / Delete -> {{m.Entity}}.Admin, {{m.Entity}}.Write, {{m.Entity}}.<İşlem>
                //   GetAll / GetById         -> {{m.Entity}}.Admin, {{m.Entity}}.Read
                // Claim'ler OperationClaimSeeds ile veritabanına yazılır.
                public static class {{m.Entity}}OperationClaims
                {
                    public const string Admin = "{{m.Entity}}.Admin";
                    public const string Write = "{{m.Entity}}.Write";
                    public const string Read = "{{m.Entity}}.Read";
                    public const string Create = "{{m.Entity}}.Create";
                    public const string Update = "{{m.Entity}}.Update";
                    public const string Delete = "{{m.Entity}}.Delete";
                }
            }
            """;

        public static string Constants(CrudModel m) => $$"""
            namespace {{Ns(m)}}.Constants
            {
                // {{GeneratedNote}}
                // Kullanıcıya dönen mesajların anahtarları; metinler Locales/{{m.Entity}}Locale.{tr,en}.yaml içinde (docs/conventions.md).
                public static class {{m.Entity}}Constants
                {
                    public const string {{m.Entity}}NotFound = "{{m.Entity}}NotFound";
                }
            }
            """;

        public static string RepositoryInterface(CrudModel m) => $$"""
            namespace ECommerce.Application.Services.Repositories
            {
                // {{GeneratedNote}}
                // {{m.Plural}} tablosu için önceden yazılmış komutların sözleşmesi.
                // Application yalnızca bu arayüzü bilir; implementasyon Persistence katmanındadır (Dependency Inversion).
                public interface I{{m.Entity}}Repository
                {
                    Task<List<Domain.Entities.{{m.Entity}}>> GetAllAsync(CancellationToken cancellationToken = default);
                    Task<Domain.Entities.{{m.Entity}}?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
                    Task AddAsync(Domain.Entities.{{m.Entity}} {{m.Var}}, CancellationToken cancellationToken = default);
                    Task UpdateAsync(Domain.Entities.{{m.Entity}} {{m.Var}}, CancellationToken cancellationToken = default);
                    Task DeleteAsync(Domain.Entities.{{m.Entity}} {{m.Var}}, CancellationToken cancellationToken = default);
                }
            }
            """;
    }
}
