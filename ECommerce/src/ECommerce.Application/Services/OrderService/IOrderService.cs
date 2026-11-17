namespace ECommerce.Application.Services.OrderService
{
    // Kural: Bir CQRS handler'ı yalnızca kendi entity'sinin repository'sini enjekte edebilir.
    // Başka bir feature'ın (ör. OrderItem) Order'a ihtiyacı varsa IOrderRepository'yi değil,
    // bu servisi kullanır.
    public interface IOrderService
    {
        Task<Domain.Entities.Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        // Sipariş toplamına farkı ekler (negatifse düşer) ve kaydeder.
        // Toplam tutarın nasıl değiştiği kuralı Order tarafında, tek yerde durur.
        Task ChangeTotalPriceAsync(Domain.Entities.Order order, decimal difference, CancellationToken cancellationToken = default);
    }
}
