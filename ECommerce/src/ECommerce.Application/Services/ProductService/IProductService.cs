namespace ECommerce.Application.Services.ProductService
{
    // Kural: Bir CQRS handler'ı yalnızca kendi entity'sinin repository'sini enjekte edebilir.
    // Başka bir feature'ın (Order, OrderItem...) Product'a ihtiyacı varsa IProductRepository'yi değil,
    // bu servisi kullanır. Servis, Product'a dair dışarıya açılan iş kurallarının tek kapısıdır.
    public interface IProductService
    {
        Task<Domain.Entities.Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        // Stok kuralları Product'a aittir; sipariş tarafı stoğa yalnızca bu metotlarla dokunur.
        bool HasEnoughStock(Domain.Entities.Product product, int amount);
        // Stok yetersizse ArgumentException fırlatır.
        Task DecreaseStockAsync(Domain.Entities.Product product, int amount, CancellationToken cancellationToken = default);
        Task IncreaseStockAsync(Domain.Entities.Product product, int amount, CancellationToken cancellationToken = default);
    }
}
