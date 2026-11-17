using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;


namespace ECommerce.Persistence.Contexts
{
    // Veritabanının C# temsili
    public class UludagDbContext : DbContext
    {
        // Aynı veritabanı durumunu farklı yerlerde kullanabilmek için.
        public UludagDbContext(DbContextOptions options) : base(options)
        {
            // Koddan çıkartıp, konfigürasyona almak lazım.
        }

        protected UludagDbContext()
        {
        }

        // Tablolar
        public DbSet<Product> Products => Set<Product>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>()
                .ToTable("Products")
                .Property(i => i.Name).HasColumnType("NVARCHAR(100)").IsRequired(false);

            base.OnModelCreating(modelBuilder);
        }
    }
}
