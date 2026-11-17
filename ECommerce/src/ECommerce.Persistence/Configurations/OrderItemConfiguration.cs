using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations
{
    // OrderItems tablosunun konfigürasyonu
    public class OrderItemConfiguration
        : IEntityTypeConfiguration<OrderItem>
    {
        public void Configure(EntityTypeBuilder<OrderItem> builder)
        {
            builder.OwnsOne(i => i.TotalPrice, price =>
            {
                price.Property(m => m.Amount)
                    .HasPrecision(18, 2);

                price.Property(m => m.Currency)
                    .HasMaxLength(3)
                    .IsRequired();
            });

            builder.Navigation(i => i.TotalPrice).IsRequired();

            // Sipariş silinirse kalemleri de silinir.
            builder.HasOne(i => i.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Siparişte geçen bir ürün silinirse sipariş geçmişi silinmemeli.
            builder.HasOne(i => i.Product)
                .WithMany()
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
