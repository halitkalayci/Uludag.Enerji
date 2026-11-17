using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.Order.Commands.Update
{
    // Geriye bool döner: true -> güncellendi, false -> sipariş bulunamadı.
    // TotalPrice burada güncellenmez; kalemlerden hesaplanan bir değerdir (OrderItem komutları yönetir).
    public class UpdateOrderCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
        public DateTime OrderDate { get; set; }

        public class UpdateOrderCommandHandler : IRequestHandler<UpdateOrderCommand, bool>
        {
            // Dependency Injection
            private readonly IOrderRepository orderRepository;
            public UpdateOrderCommandHandler(IOrderRepository orderRepository)
            {
                this.orderRepository = orderRepository;
            }

            public async Task<bool> Handle(UpdateOrderCommand request, CancellationToken cancellationToken)
            {
                Domain.Entities.Order? order = await orderRepository.GetByIdAsync(request.Id, cancellationToken);

                if (order is null)
                    return false;

                // Mapping
                order.OrderDate = request.OrderDate;

                await orderRepository.UpdateAsync(order, cancellationToken);
                return true;
            }
        }
    }
}
