using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations
{
    // Users tablosunun konfigürasyonu.
    public class UserConfiguration
        : IEntityTypeConfiguration<User>
    {
        // RFC 5321'e göre bir e-posta adresi en fazla 320 karakterdir (64 yerel kısım + "@" + 255 alan adı).
        // Sınır gerekli: SQL Server nvarchar(max) bir kolona index koyamaz.
        public const int EmailMaxLength = 320;

        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder
                .Property(u => u.Email)
                .HasMaxLength(EmailMaxLength)
                .IsRequired();

            // BR-8: e-posta benzersizdir — kontrol (UserEmailCannotExist) ile kayıt arasındaki yarışta da.
            // Varsayılan collation büyük/küçük harfe duyarsızdır: "A@x.com" ile "a@x.com" aynı kayıt sayılır.
            builder
                .HasIndex(u => u.Email)
                .IsUnique();
        }
    }
}
