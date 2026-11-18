using ECommerce.Domain.Entities;
using ECommerce.Persistence.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations
{
    // OperationClaims tablosunun konfigürasyonu
    public class OperationClaimConfiguration
        : IEntityTypeConfiguration<OperationClaim>
    {
        public void Configure(EntityTypeBuilder<OperationClaim> builder)
        {
            builder
                .Property(c => c.Name)
                .HasMaxLength(100)
                .IsRequired();

            // Aynı isimde iki claim olamaz: yetki kontrolü isimle (metinsel) yapılır.
            builder
                .HasIndex(c => c.Name)
                .IsUnique();

            // Seed Data
            builder.HasData(OperationClaimSeeds.All);
        }
    }
}
