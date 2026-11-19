namespace ECommerce.Application.Services.Repositories
{
    // User tablosu için önceden yazılmış komutların sözleşmesi.
    // Implementasyon Persistence katmanındadır (Dependency Inversion).
    public interface IUserRepository
    {
        Task<Domain.Entities.User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<Domain.Entities.User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task AddAsync(Domain.Entities.User user, CancellationToken cancellationToken = default);
    }
}
