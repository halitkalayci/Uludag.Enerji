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
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<User> Users => Set<User>();
        public DbSet<OperationClaim> OperationClaims => Set<OperationClaim>();
        public DbSet<UserOperationClaim> UserOperationClaims => Set<UserOperationClaim>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            /*modelBuilder.Entity<Product>()
                .ToTable("Products")
                .Property(i => i.Name).HasColumnType("NVARCHAR(100)").IsRequired(false);
            */

            // Assembly'deki (çalışma zamanındaki kodlar) tüm IEntityTypeConfiguration implementasyonlarını bul ve uygula.
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(UludagDbContext).Assembly);

            base.OnModelCreating(modelBuilder);
        }
    }
}
