namespace Uludag.Core.Mediation
{
    /// <summary>
    /// <typeparamref name="TRequest"/> isteğini işleyip <typeparamref name="TResponse"/> döndüren handler.
    /// Her istek tipi için tek bir handler kaydedilmelidir.
    /// </summary>
    public interface IRequestHandler<in TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Geriye değer döndürmeyen <typeparamref name="TRequest"/> isteğini işleyen handler.
    /// Implementasyon sadece <c>Task Handle(...)</c> yazar; <see cref="Unit"/> dönüşü otomatik yapılır.
    /// </summary>
    public interface IRequestHandler<in TRequest> : IRequestHandler<TRequest, Unit>
        where TRequest : IRequest
    {
        new Task Handle(TRequest request, CancellationToken cancellationToken);

        async Task<Unit> IRequestHandler<TRequest, Unit>.Handle(TRequest request, CancellationToken cancellationToken)
        {
            await Handle(request, cancellationToken).ConfigureAwait(false);
            return Unit.Value;
        }
    }
}
