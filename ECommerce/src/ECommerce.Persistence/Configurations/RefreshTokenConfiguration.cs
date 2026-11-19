using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations
{
    // RefreshTokens tablosunun konfigürasyonu (User ile bire-çok).
    public class RefreshTokenConfiguration
        : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder
                .HasOne(r => r.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // SHA-256 -> 64 karakter hex. Token bu özetle aranır; aynı özet iki kez olamaz.
            builder
                .Property(r => r.TokenHash)
                .HasMaxLength(64)
                .IsFixedLength()
                .IsUnicode(false)
                .IsRequired();

            builder
                .HasIndex(r => r.TokenHash)
                .IsUnique();

            // Tekrar kullanım tespitinde ailenin tüm token'ları tek SQL ile revoke edilir.
            builder
                .HasIndex(r => r.FamilyId);

            // Enum DB'ye sayı değil ad olarak yazılır ("Rotated", "RevokedByUser", "ReuseDetected").
            builder
                .Property(r => r.RevocationReason)
                .HasConversion<string>()
                .HasMaxLength(32);

            // ReplacedByTokenId bilerek foreign key değildir: rotasyonda eski kayıt, yeni kayıt eklenmeden önce
            // atomik olarak revoke edilir (aynı token'ın eşzamanlı iki kullanımını engellemek için).
        }
    }
}
