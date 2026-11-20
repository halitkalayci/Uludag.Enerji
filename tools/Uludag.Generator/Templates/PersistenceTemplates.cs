using Uludag.Generator.Generation;
using static Uludag.Generator.Templates.TemplateText;

namespace Uludag.Generator.Templates
{
    // ECommerce.Persistence şablonları. Kalıp: ProductRepository ve Order/ProductConfiguration.
    internal static class PersistenceTemplates
    {
        public static string Repository(CrudModel m) => $$"""
            using ECommerce.Application.Services.Repositories;
            using ECommerce.Domain.Entities;
            using ECommerce.Persistence.Contexts;
            using Microsoft.EntityFrameworkCore;

            namespace ECommerce.Persistence.Repositories
            {
                // {{GeneratedNote}}
                // I{{m.Entity}}Repository'nin EF Core ile yazılmış hali. Context'e yalnızca bu katman dokunur.
                public class {{m.Entity}}Repository : I{{m.Entity}}Repository
                {
                    private readonly UludagDbContext uludagDbContext;
                    public {{m.Entity}}Repository(UludagDbContext uludagDbContext)
                    {
                        this.uludagDbContext = uludagDbContext;
                    }

                    public async Task<List<{{m.Entity}}>> GetAllAsync(CancellationToken cancellationToken = default)
                    {
                        return await uludagDbContext.{{m.Plural}}
                            .AsNoTracking()
                            .ToListAsync(cancellationToken);
                    }

                    public async Task<{{m.Entity}}?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
                    {
                        return await uludagDbContext.{{m.Plural}}
                            .FirstOrDefaultAsync({{m.Lambda}} => {{m.Lambda}}.Id == id, cancellationToken);
                    }

                    public async Task AddAsync({{m.Entity}} {{m.Var}}, CancellationToken cancellationToken = default)
                    {
                        await uludagDbContext.{{m.Plural}}.AddAsync({{m.Var}}, cancellationToken);
                        await uludagDbContext.SaveChangesAsync(cancellationToken);
                    }

                    public async Task UpdateAsync({{m.Entity}} {{m.Var}}, CancellationToken cancellationToken = default)
                    {
                        uludagDbContext.{{m.Plural}}.Update({{m.Var}});
                        await uludagDbContext.SaveChangesAsync(cancellationToken);
                    }

                    public async Task DeleteAsync({{m.Entity}} {{m.Var}}, CancellationToken cancellationToken = default)
                    {
                        uludagDbContext.{{m.Plural}}.Remove({{m.Var}});
                        await uludagDbContext.SaveChangesAsync(cancellationToken);
                    }
                }
            }
            """;

        public static string Configuration(CrudModel m)
        {
            // Money owned type olarak eşlenir; aksi halde EF onu anahtarsız bir entity sanar (spec R-2, G-4).
            var money = m.MoneyProperties.SelectMany(p => new[]
            {
                $"builder.OwnsOne({m.Lambda} => {m.Lambda}.{p.Name}, money =>",
                "{",
                "    money.Property(x => x.Amount)",
                "        .HasPrecision(18, 2);",
                "",
                "    money.Property(x => x.Currency)",
                "        .HasMaxLength(3)",
                "        .IsRequired();",
                "});",
                $"builder.Navigation({m.Lambda} => {m.Lambda}.{p.Name}).IsRequired();",
                ""
            }).SkipLast(1);

            return $$"""
                using ECommerce.Domain.Entities;
                using Microsoft.EntityFrameworkCore;
                using Microsoft.EntityFrameworkCore.Metadata.Builders;

                namespace ECommerce.Persistence.Configurations
                {
                    // {{GeneratedNote}}
                    // {{m.Plural}} tablosunun konfigürasyonu (iskelet). Uzunluk, indeks, ilişki ve seed gibi ayarlar elle eklenir.
                    public class {{m.Entity}}Configuration : IEntityTypeConfiguration<{{m.Entity}}>
                    {
                        public void Configure(EntityTypeBuilder<{{m.Entity}}> builder)
                        {
                            builder.HasKey({{m.Lambda}} => {{m.Lambda}}.Id);

                {{Block(money, 12)}}
                        }
                    }
                }
                """;
        }
    }
}
