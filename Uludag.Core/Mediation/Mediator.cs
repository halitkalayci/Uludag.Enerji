using System.Collections.Concurrent;
using Uludag.Core.Mediation.Internal;

namespace Uludag.Core.Mediation
{
    /// <summary>
    /// <see cref="IMediator"/>'un varsayılan implementasyonu.
    /// Handler'ı ve pipeline behavior'larını DI container'dan çözüp sırayla çalıştırır.
    /// </summary>q
    public sealed class Mediator : IMediator
    {
        // İstek tipi başına wrapper bir kez reflection ile oluşturulur, sonra cache'ten gelir.
        private static readonly ConcurrentDictionary<Type, object> RequestWrappers = new();

        private readonly IServiceProvider _serviceProvider;

        public Mediator(IServiceProvider serviceProvider)
        {   
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var wrapper = (RequestHandlerWrapper<TResponse>)RequestWrappers.GetOrAdd(
                request.GetType(),
                static requestType => Activator.CreateInstance(
                    typeof(RequestHandlerWrapperImpl<,>).MakeGenericType(requestType, typeof(TResponse)))!);

            return wrapper.Handle(request, _serviceProvider, cancellationToken);
        }
    }
}
