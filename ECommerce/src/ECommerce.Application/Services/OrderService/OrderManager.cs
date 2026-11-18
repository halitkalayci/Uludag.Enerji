using ECommerce.Application.Features.Order.Rules;
using ECommerce.Application.Services.Repositories;

namespace ECommerce.Application.Services.OrderService
{
    // IOrderService'in implementasyonu. Yalnızca kendi repository'sini (IOrderRepository)
    // ve kendi feature'ının kurallarını (OrderBusinessRules) kullanır.
    public class OrderManager : IOrderService
    {
        // Dependency Injection
        private readonly IOrderRepository orderRepository;
        private readonly OrderBusinessRules orderBusinessRules;
        public OrderManager(IOrderRepository orderRepository, OrderBusinessRules orderBusinessRules)
        {
            this.orderRepository = orderRepository;
            this.orderBusinessRules = orderBusinessRules;
        }

        public async Task<Domain.Entities.Order> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            Domain.Entities.Order? order = await orderRepository.GetByIdAsync(id, cancellationToken);
            orderBusinessRules.OrderShouldExistWhenSelected(order);
            return order;
        }

        public async Task ChangeTotalPriceAsync(Domain.Entities.Order order, decimal difference, CancellationToken cancellationToken = default)
        {
            order.TotalPrice = new Domain.ValueObjects.Money(
                order.TotalPrice.Amount + difference,
                order.TotalPrice.Currency);

            await orderRepository.UpdateAsync(order, cancellationToken);
        }

        public async Task ChangeTotalPriceIfOrderExistsAsync(Guid orderId, decimal difference, CancellationToken cancellationToken = default)
        {
            Domain.Entities.Order? order = await orderRepository.GetByIdAsync(orderId, cancellationToken);
            if (order is null)
                return;

            await ChangeTotalPriceAsync(order, difference, cancellationToken);
        }
    }
}
