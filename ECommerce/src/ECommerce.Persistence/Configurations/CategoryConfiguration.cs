using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations
{
    // uludag-gen ile üretildi (spec 0005); spec 0009 ile düzenlendi. Categories tablosunun konfigürasyonu.
    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        // BR-12: büyük/küçük harf duyarsız karşılaştırma. Sunucunun varsayılan collation'ına bırakılmaz (B-17):
        // kolon açıkça CI collation ile oluşturulur; hem unique index hem kuraldaki ad sorgusu bunu kullanır.
        private const string CaseInsensitiveCollation = "SQL_Latin1_General_CP1_CI_AS";

        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.HasKey(c => c.Id);

            builder
                .Property(c => c.Name)
                .HasMaxLength(Category.NameMaxLength)
                .UseCollation(CaseInsensitiveCollation)
                .IsRequired();

            // BR-12: ad benzersizdir — kontrol (CategoryNameCannotExist) ile kayıt arasındaki yarışta da.
            builder
                .HasIndex(c => c.Name)
                .IsUnique();
        }
    }
}
