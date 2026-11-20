using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Persistence.Configurations
{
    // Products tablosunun konfigürasyonu
    public class ProductConfiguration 
        : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            // Seed Data
            // Price owned type olduğu için Product ile birlikte seed edilemez:
            // Product'ın kendi alanları burada, Price aşağıda OwnsOne içinde ProductId ile seed edilir.
            var seedProductId = Guid.Parse("2dcb27b9-06cf-4053-a423-c2e94ed048a6");

            builder.HasData(new Product
            {
                Id = seedProductId,
                Name = "Deneme ürün",
                Stock = 100
            });
            //


            builder
                .Property(i => i.Name)
                .HasMaxLength(200)
                .IsRequired(false);

            builder.OwnsOne(p => p.Price, price =>
            {
                price.Property(m => m.Amount)
                    .HasPrecision(18, 2);

                price.Property(m => m.Currency)
                    .HasMaxLength(3)
                    .IsRequired();

                // Owned type seed'i: ProductId, EF'in owner'a bağlamak için kullandığı shadow key.
                price.HasData(new { ProductId = seedProductId, Amount = 500m, Currency = "TRY" });
            });

            builder.Navigation(p => p.Price).IsRequired();

            // BR-13: ürünün sıfır ya da bir kategorisi vardır. BR-14: ürünü olan kategori silinemez —
            // kural CategoryBusinessRules'ta; Restrict, kural ile silme arasındaki yarışta veriyi korur.
            builder
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
