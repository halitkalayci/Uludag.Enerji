using ECommerce.Application.Services.Repositories;
using ECommerce.Persistence.Contexts;
using ECommerce.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Persistence
{
    public static class PersistenceServiceRegisteration
    {
        public static IServiceCollection AddPersistenceServices(this IServiceCollection services)
        {
            // bağlandığımız db..
            //services.AddDbContext<UludagDbContext>(opt => opt.UseNpgsql("Host=localhost;Database=uludag;Username=postgres;Password=postgres"));
            services.AddDbContext<UludagDbContext>(opt => opt.UseSqlServer("Server=127.0.0.1,1433;Database=uludag;User Id=sa;Password=Admin123!;TrustServerCertificate=True;"));

            // Repositories -> Application arayüzü ister, Persistence somut sınıfı verir.
            // Scoped: DbContext ile aynı ömür (istek başına bir tane).
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IOrderItemRepository, OrderItemRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUserOperationClaimRepository, UserOperationClaimRepository>();

            return services;
        }
    }
}
