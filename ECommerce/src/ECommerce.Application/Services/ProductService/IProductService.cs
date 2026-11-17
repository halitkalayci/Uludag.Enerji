namespace ECommerce.Application.Services.ProductService
{
    // Kural: Bir CQRS handler'ı yalnızca kendi entity'sinin repository'sini enjekte edebilir.
    // Başka bir feature'ın (Order, OrderItem...) Product'a ihtiyacı varsa IProductRepository'yi değil,
    // bu servisi kullanır. Servis, Product'a dair dışarıya açılan iş kurallarının tek kapısıdır.
    public interface IProductService
    {
        Task<Domain.Entities.Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    }
}
