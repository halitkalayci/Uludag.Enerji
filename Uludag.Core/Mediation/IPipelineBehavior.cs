namespace Uludag.Core.Mediation
{
    /// <summary>
    /// Pipeline'da bir sonraki adımı (bir sonraki behavior ya da en sonda handler'ın kendisi) temsil eder.
    /// </summary>
    public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

    /// <summary>
    /// İstek handler'a ulaşmadan önce/sonra araya giren davranış (logging, validation, transaction, cache...).
    /// Kayıt sırasına göre dıştan içe çalışır: ilk kaydedilen behavior en dıştadır.
    /// </summary>
    public interface IPipelineBehavior<in TRequest, TResponse>
        where TRequest : notnull
    {
        Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
    }
}
