using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations
{
    // uludag-gen ile üretildi (spec 0005); serbestçe düzenlenebilir.
    // Categories tablosunun konfigürasyonu (iskelet). Uzunluk, indeks, ilişki ve seed gibi ayarlar elle eklenir.
    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.HasKey(c => c.Id);
        }
    }
}
