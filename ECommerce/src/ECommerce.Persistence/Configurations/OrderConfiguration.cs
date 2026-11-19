using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations
{
    // Orders tablosunun konfigürasyonu (User ile bire-çok).
    public class OrderConfiguration
        : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            // Restrict: siparişler satış kaydıdır; kullanıcı silinince sessizce silinmemeli.
            // Siparişi olan bir kullanıcı silinmek istenirse veritabanı FK hatası verir.
            builder
                .HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);

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
