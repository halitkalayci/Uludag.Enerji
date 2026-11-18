using ECommerce.Application.Features.Order.Rules;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.Order.Commands.Update
{
    // Sipariş bulunamazsa NotFoundException fırlar.
    // TotalPrice burada güncellenmez; kalemlerden hesaplanan bir değerdir (OrderItem komutları yönetir).
    public class UpdateOrderCommand : IRequest
    {
        public Guid Id { get; set; }
        public DateTime OrderDate { get; set; }

        public class UpdateOrderCommandHandler : IRequestHandler<UpdateOrderCommand>
        {
            // Dependency Injection
            private readonly IOrderRepository orderRepository;
            private readonly OrderBusinessRules orderBusinessRules;
            public UpdateOrderCommandHandler(IOrderRepository orderRepository, OrderBusinessRules orderBusinessRules)
            {
                this.orderRepository = orderRepository;
                this.orderBusinessRules = orderBusinessRules;
            }

            public async Task Handle(UpdateOrderCommand request, CancellationToken cancellationToken)
            {
                Domain.Entities.Order? order = await orderRepository.GetByIdAsync(request.Id, cancellationToken);
                orderBusinessRules.OrderShouldExistWhenSelected(order);

                // Mapping
                order.OrderDate = request.OrderDate;

                await orderRepository.UpdateAsync(order, cancellationToken);
            }
        }
    }
}
