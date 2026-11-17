using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations
{
    // Orders tablosunun konfigürasyonu
    public class OrderConfiguration
        : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.OwnsOne(o => o.TotalPrice, price =>
            {
                price.Property(m => m.Amount)
                    .HasPrecision(18, 2);

                price.Property(m => m.Currency)
                    .HasMaxLength(3)
                    .IsRequired();
            });

            builder.Navigation(o => o.TotalPrice).IsRequired();
        }
    }
}
