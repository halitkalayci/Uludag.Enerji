using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations
{
    // OtpAuthenticators tablosunun konfigürasyonu (User ile bire-bir).
    public class OtpAuthenticatorConfiguration
        : IEntityTypeConfiguration<OtpAuthenticator>
    {
        public void Configure(EntityTypeBuilder<OtpAuthenticator> builder)
        {
            builder
                .HasOne(o => o.User)
                .WithOne(u => u.OtpAuthenticator)
                .HasForeignKey<OtpAuthenticator>(o => o.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Bir kullanıcının en fazla bir authenticator kaydı olur.
            builder
                .HasIndex(o => o.UserId)
                .IsUnique();

            // OtpNet 20 byte (160 bit) anahtar üretir.
            builder
                .Property(o => o.SecretKey)
                .HasMaxLength(64)
                .IsRequired();
        }
    }
}
