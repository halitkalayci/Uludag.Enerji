using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.Order.Commands.Delete
{
    // Geriye bool döner: true -> silindi, false -> sipariş bulunamadı.
    // Sipariş silinince kalemleri de silinir.
    public class DeleteOrderCommand : IRequest<bool>
    {
        public Guid Id { get; set; }

        public class DeleteOrderCommandHandler : IRequestHandler<DeleteOrderCommand, bool>
        {
            // Dependency Injection
            private readonly IOrderRepository orderRepository;
            public DeleteOrderCommandHandler(IOrderRepository orderRepository)
            {
                this.orderRepository = orderRepository;
            }

            public async Task<bool> Handle(DeleteOrderCommand request, CancellationToken cancellationToken)
            {
                Domain.Entities.Order? order = await orderRepository.GetByIdAsync(request.Id, cancellationToken);

                if (order is null)
                    return false;

                await orderRepository.DeleteAsync(order, cancellationToken);
                return true;
            }
        }
    }
}
