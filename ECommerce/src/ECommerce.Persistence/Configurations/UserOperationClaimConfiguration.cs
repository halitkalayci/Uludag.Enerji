using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations
{
    // UserOperationClaims (User <-> OperationClaim ara tablosu) konfigürasyonu
    public class UserOperationClaimConfiguration
        : IEntityTypeConfiguration<UserOperationClaim>
    {
        public void Configure(EntityTypeBuilder<UserOperationClaim> builder)
        {
            builder
                .HasOne(uoc => uoc.User)
                .WithMany(u => u.UserOperationClaims)
                .HasForeignKey(uoc => uoc.UserId);

            builder
                .HasOne(uoc => uoc.OperationClaim)
                .WithMany(c => c.UserOperationClaims)
                .HasForeignKey(uoc => uoc.OperationClaimId);

            // Bir kullanıcıya aynı claim iki kez atanamaz.
            builder
                .HasIndex(uoc => new { uoc.UserId, uoc.OperationClaimId })
                .IsUnique();
        }
    }
}
