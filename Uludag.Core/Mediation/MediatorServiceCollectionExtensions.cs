using System.Reflection;
using Uludag.Core.Mediation;

// Kullanan projeler ekstra using yazmadan services.AddMediator(...) diyebilsin diye
// uzantı, DI'ın kendi namespace'inde tanımlanır.
namespace Microsoft.Extensions.DependencyInjection
{
    public static class MediatorServiceCollectionExtensions
    {
        /// <summary>
        /// <see cref="IMediator"/>'u kaydeder ve verilen assembly'lerdeki tüm
        /// <see cref="IRequestHandler{TRequest,TResponse}"/> implementasyonlarını bulup ekler.
        /// </summary>
        /// <remarks>
        /// Pipeline behavior'ları ayrıca, istenen sırayla kaydedilir (ilk eklenen en dışta çalışır):
        /// <c>services.AddTransient(typeof(IPipelineBehavior&lt;,&gt;), typeof(LoggingBehavior&lt;,&gt;));</c>
        /// </remarks>
        public static IServiceCollection AddMediator(this IServiceCollection services, params Assembly[] assemblies)
        {
            ArgumentNullException.ThrowIfNull(services);
            if (assemblies is null || assemblies.Length == 0)
                throw new ArgumentException("Handler'ların aranacağı en az bir assembly verilmelidir.", nameof(assemblies));

            services.AddScoped<IMediator, Mediator>();

            foreach (var type in assemblies.Distinct().SelectMany(GetLoadableTypes))
            {
                if (!type.IsClass || type.IsAbstract || type.IsGenericTypeDefinition)
                    continue;

                foreach (var serviceType in type.GetInterfaces())
                {
                    if (serviceType.IsGenericType &&
                        serviceType.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))
                    {
                        services.AddTransient(serviceType, type);
                    }
                }
            }

            return services;
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.OfType<Type>();
            }
        }
    }
}
