using ECommerce.Application.Services.Repositories;

namespace ECommerce.Application.Services.OrderService
{
    // IOrderService'in implementasyonu. Yalnızca kendi repository'sini (IOrderRepository) kullanır.
    public class OrderManager : IOrderService
    {
        // Dependency Injection
        private readonly IOrderRepository orderRepository;
        public OrderManager(IOrderRepository orderRepository)
        {
            this.orderRepository = orderRepository;
        }

        public async Task<Domain.Entities.Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await orderRepository.GetByIdAsync(id, cancellationToken);
        }

        public async Task ChangeTotalPriceAsync(Domain.Entities.Order order, decimal difference, CancellationToken cancellationToken = default)
        {
            order.TotalPrice = new Domain.ValueObjects.Money(
                order.TotalPrice.Amount + difference,
                order.TotalPrice.Currency);

            await orderRepository.UpdateAsync(order, cancellationToken);
        }
    }
}
