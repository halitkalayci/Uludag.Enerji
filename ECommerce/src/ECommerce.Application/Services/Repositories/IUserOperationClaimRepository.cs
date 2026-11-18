namespace ECommerce.Application.Services.Repositories
{
    // UserOperationClaim tablosu için önceden yazılmış komutların sözleşmesi.
    // Implementasyon Persistence katmanındadır (Dependency Inversion).
    public interface IUserOperationClaimRepository
    {
        // Kullanıcının sahip olduğu claim'lerin yalnızca adları (ör. "Product.Read") -> token'a bunlar yazılır.
        Task<List<string>> GetOperationClaimNamesByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
