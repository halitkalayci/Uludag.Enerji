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
            services.AddDbContext<UludagDbContext>(opt => opt.UseInMemoryDatabase("Db"));

            // Repositories -> Application arayüzü ister, Persistence somut sınıfı verir.
            // Scoped: DbContext ile aynı ömür (istek başına bir tane).
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IOrderItemRepository, OrderItemRepository>();

            return services;
        }
    }
}
