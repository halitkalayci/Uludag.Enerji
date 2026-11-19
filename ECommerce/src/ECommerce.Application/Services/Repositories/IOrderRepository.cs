namespace ECommerce.Application.Services.Repositories
{
    // Order tablosu için önceden yazılmış komutların sözleşmesi.
    // Implementasyon Persistence katmanındadır (Dependency Inversion).
    public interface IOrderRepository
    {
        Task<List<Domain.Entities.Order>> GetAllAsync(CancellationToken cancellationToken = default);
        // Yalnızca verilen kullanıcıya ait siparişler.
        Task<List<Domain.Entities.Order>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<Domain.Entities.Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        // Siparişi kalemleriyle (Items) birlikte getirir.
        Task<Domain.Entities.Order?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default);
        Task AddAsync(Domain.Entities.Order order, CancellationToken cancellationToken = default);
        Task UpdateAsync(Domain.Entities.Order order, CancellationToken cancellationToken = default);
        // Siparişi kalemleriyle (OrderItem) birlikte siler.
        Task DeleteAsync(Domain.Entities.Order order, CancellationToken cancellationToken = default);
    }
}
