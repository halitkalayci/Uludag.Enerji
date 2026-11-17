using ECommerce.Application.Services.OrderService;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.OrderItem.Commands.Delete
{
    // Geriye bool döner: true -> silindi, false -> kalem bulunamadı.
    public class DeleteOrderItemCommand : IRequest<bool>
    {
        public Guid Id { get; set; }

        public class DeleteOrderItemCommandHandler : IRequestHandler<DeleteOrderItemCommand, bool>
        {
            // Dependency Injection
            // Kendi entity'si -> repository. Başka entity (Order) -> servis.
            private readonly IOrderItemRepository orderItemRepository;
            private readonly IOrderService orderService;
            public DeleteOrderItemCommandHandler(IOrderItemRepository orderItemRepository, IOrderService orderService)
            {
                this.orderItemRepository = orderItemRepository;
                this.orderService = orderService;
            }

            public async Task<bool> Handle(DeleteOrderItemCommand request, CancellationToken cancellationToken)
            {
                Domain.Entities.OrderItem? orderItem = await orderItemRepository.GetByIdAsync(request.Id, cancellationToken);
                if (orderItem is null)
                    return false;

                Domain.Entities.Order? order = await orderService.GetByIdAsync(orderItem.OrderId, cancellationToken);

                await orderItemRepository.DeleteAsync(orderItem, cancellationToken);

                // Sipariş toplamından kalemin tutarını düş.
                if (order is not null)
                    await orderService.ChangeTotalPriceAsync(order, -orderItem.TotalPrice.Amount, cancellationToken);

                return true;
            }
        }
    }
}
