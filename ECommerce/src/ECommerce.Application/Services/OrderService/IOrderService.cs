namespace ECommerce.Application.Services.OrderService
{
    // Kural: Bir CQRS handler'ı yalnızca kendi entity'sinin repository'sini enjekte edebilir.
    // Başka bir feature'ın (ör. OrderItem) Order'a ihtiyacı varsa IOrderRepository'yi değil,
    // bu servisi kullanır. Kuralların kendisi OrderBusinessRules'tadır; servis onları çağırır.
    public interface IOrderService
    {
        // Siparişi getirir. Sipariş yoksa NotFoundException fırlatır (null dönmez).
        Task<Domain.Entities.Order> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        // Sipariş toplamına farkı ekler (negatifse düşer) ve kaydeder.
        // Toplam tutarın nasıl değiştiği kuralı Order tarafında, tek yerde durur.
        Task ChangeTotalPriceAsync(Domain.Entities.Order order, decimal difference, CancellationToken cancellationToken = default);
        // Sipariş bu arada silinmişse düzeltilecek toplam yoktur, hiçbir şey yapılmaz.
        Task ChangeTotalPriceIfOrderExistsAsync(Guid orderId, decimal difference, CancellationToken cancellationToken = default);
    }
}
