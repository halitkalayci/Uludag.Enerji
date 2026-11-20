namespace ECommerce.Application.Services.ProductService
{
    // Kural: Bir CQRS handler'ı yalnızca kendi entity'sinin repository'sini enjekte edebilir.
    // Başka bir feature'ın (Order, OrderItem...) Product'a ihtiyacı varsa IProductRepository'yi değil,
    // bu servisi kullanır. Servis, Product'a dair dışarıya açılan iş kurallarının tek kapısıdır.
    // Kuralların kendisi ProductBusinessRules'tadır; servis onları çağırır.
    public interface IProductService
    {
        // Ürünü getirir. Ürün yoksa NotFoundException fırlatır (null dönmez).
        Task<Domain.Entities.Product> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        // Stok kuralları Product'a aittir; sipariş tarafı stoğa yalnızca bu metotlarla dokunur.
        // Stok istenen adedi karşılamıyorsa BusinessException fırlatır. Hiçbir şeyi değiştirmez;
        // birden fazla ürünün stoğu düşülmeden önce hepsini kontrol etmek için kullanılır.
        void StockShouldBeEnough(Domain.Entities.Product product, int amount);
        // Stok yetersizse BusinessException fırlatır.
        Task DecreaseStockAsync(Domain.Entities.Product product, int amount, CancellationToken cancellationToken = default);
        Task IncreaseStockAsync(Domain.Entities.Product product, int amount, CancellationToken cancellationToken = default);
        // Sipariş adedindeki değişime göre stoğu düzeltir:
        // fark pozitifse (adet arttı) fark kadar düşer, negatifse (adet azaldı) fark kadar iade eder.
        Task ChangeStockByAmountDifferenceAsync(Domain.Entities.Product product, int amountDifference, CancellationToken cancellationToken = default);
        // Stok iadesi: ürün bu arada silinmişse iade edilecek stok yoktur, hiçbir şey yapılmaz.
        Task IncreaseStockIfProductExistsAsync(Guid productId, int amount, CancellationToken cancellationToken = default);

        // Kategoriye bağlı en az bir ürün var mı? (Category silme kuralı BR-14 ürün tablosuna bu kapıdan bakar.)
        Task<bool> AnyInCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);
    }
}
