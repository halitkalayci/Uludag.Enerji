namespace Uludag.Core.Mediation
{
    /// <summary>
    /// Geriye <typeparamref name="TResponse"/> döndüren bir istek (Command / Query).
    /// </summary>
    public interface IRequest<TResponse>
    {
    }

    /// <summary>
    /// Geriye değer döndürmeyen bir istek (ör. Create/Delete komutları).
    /// </summary>
    public interface IRequest : IRequest<Unit>
    {
    }
}
