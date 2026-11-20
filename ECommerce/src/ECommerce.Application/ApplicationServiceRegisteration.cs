using ECommerce.Application.Services.CategoryService;
using ECommerce.Application.Features.Product.Commands.Create;
using ECommerce.Application.Features.Product.Rules;
using ECommerce.Application.Services.OrderService;
using ECommerce.Application.Services.ProductService;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Uludag.Core.Mediation;
using Uludag.Core.Mediation.Behaviors;

namespace ECommerce.Application
{
    // Kendi bağımlılıklarını sisteme kendisi tanıtır.
    public static class ApplicationServiceRegisteration
    {
        // Extension Method
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddMediator(typeof(CreateProductCommand).Assembly);
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthenticationBehavior<,>)); // kapatmak 1 satır
            // Sıra önemli: önce kimlik (Authentication), sonra yetki (Authorization).
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));


            // Business servisleri -> feature'lar arası erişim repository ile değil, bu servislerle yapılır.
            // Scoped: kullandıkları repository/DbContext ile aynı ömür.
            services.AddScoped<IProductService, ProductManager>();

            services.AddScoped<IOrderService, OrderManager>();

            services.AddScoped<ICategoryService, CategoryManager>();

            // kodda satırı 100x > 1
            services.AddSubClassesOfType(Assembly.GetExecutingAssembly(), typeof(BaseBusinessRules));

            // Business rules X satır
            // Servis X satır


            return services;
        }

        public static IServiceCollection AddSubClassesOfType(
            this IServiceCollection services,
            Assembly assembly,
            Type type,
            Func<IServiceCollection, Type, IServiceCollection>? addWithLifeCycle = null)
        {
            var types = assembly.GetTypes().Where(t => t.IsSubclassOf(type) && type != t).ToList();
            foreach (Type? item in types)
                if (addWithLifeCycle == null)
                    services.AddScoped(item);
                else
                    addWithLifeCycle(services, type);
            return services;
        }
    }
}
