using ECommerce.Application.Features.Product.Commands.Create;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
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
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

            return services;
        }
    }
}
