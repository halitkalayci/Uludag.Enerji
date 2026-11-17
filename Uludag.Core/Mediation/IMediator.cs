namespace Uludag.Core.Mediation
{
    /// <summary>
    /// Ortak iletişim noktası: isteği (Command / Query) ilgili handler'a iletir.
    /// Gönderen taraf handler'ları tanımaz; yalnızca Mediator'u tanır.
    /// </summary>
    public interface IMediator
    {
        Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
    }
}
