namespace Uludag.Core.Mediation.Internal
{
    // Mediator.Send yalnızca TResponse'u bilir; TRequest'in somut tipini bilmez.
    // Bu wrapper, çalışma zamanındaki somut tiple kapatılarak (closed generic) handler'ı
    // ve pipeline behavior'larını tip güvenli şekilde çözer.
    internal abstract class RequestHandlerWrapper<TResponse>
    {
        public abstract Task<TResponse> Handle(
            IRequest<TResponse> request, IServiceProvider serviceProvider, CancellationToken cancellationToken);
    }

    internal sealed class RequestHandlerWrapperImpl<TRequest, TResponse> : RequestHandlerWrapper<TResponse>
        where TRequest : IRequest<TResponse>
    {
        public override Task<TResponse> Handle(
            IRequest<TResponse> request, IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            // EN ÖNEMLİ MEDİATOR KISIMI
            var typedRequest = (TRequest)request;

            var handler = serviceProvider.GetService(typeof(IRequestHandler<TRequest, TResponse>))
                as IRequestHandler<TRequest, TResponse>
                ?? throw new InvalidOperationException(
                    $"'{typeof(TRequest).FullName}' isteği için kayıtlı bir " +
                    $"IRequestHandler<{typeof(TRequest).Name}, {typeof(TResponse).Name}> bulunamadı.");

            RequestHandlerDelegate<TResponse> pipeline = () => handler.Handle(typedRequest, cancellationToken);

            var behaviors = serviceProvider.GetService(typeof(IEnumerable<IPipelineBehavior<TRequest, TResponse>>))
                as IEnumerable<IPipelineBehavior<TRequest, TResponse>>;

            if (behaviors is not null)
            {
                // Sondan başa sarmalanır: ilk kaydedilen behavior en dışta çalışır.
                foreach (var behavior in behaviors.Reverse())
                {
                    var next = pipeline;
                    pipeline = () => behavior.Handle(typedRequest, next, cancellationToken);
                }
            }

            return pipeline();
            //
        }
    }
}
