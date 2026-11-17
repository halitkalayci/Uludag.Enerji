namespace ECommerce.Application.Services.Repositories
{
    // OrderItem tablosu için önceden yazılmış komutların sözleşmesi.
    // Implementasyon Persistence katmanındadır (Dependency Inversion).
    public interface IOrderItemRepository
    {
        Task<List<Domain.Entities.OrderItem>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Domain.Entities.OrderItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task AddAsync(Domain.Entities.OrderItem orderItem, CancellationToken cancellationToken = default);
        Task UpdateAsync(Domain.Entities.OrderItem orderItem, CancellationToken cancellationToken = default);
        Task DeleteAsync(Domain.Entities.OrderItem orderItem, CancellationToken cancellationToken = default);
    }
}
