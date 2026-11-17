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
            builder
                .Property(i => i.Name)
                .HasMaxLength(100)
                .IsRequired(false);

            builder.OwnsOne(p => p.Price, price =>
            {
                price.Property(m => m.Amount)
                    .HasPrecision(18, 2);

                price.Property(m => m.Currency)
                    .HasMaxLength(3)
                    .IsRequired();
            });

            builder.Navigation(p => p.Price).IsRequired();
        }
    }
}
