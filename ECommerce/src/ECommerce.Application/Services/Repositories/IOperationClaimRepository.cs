namespace ECommerce.Application.Services.Repositories
{
    // OperationClaim tablosu için önceden yazılmış komutların sözleşmesi.
    // Implementasyon Persistence katmanındadır (Dependency Inversion).
    public interface IOperationClaimRepository
    {
        // Claim adıyla (ör. "Product.Read") arama. Yoksa null.
        Task<Domain.Entities.OperationClaim?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    }
}
