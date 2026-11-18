using ECommerce.Application.Features.Order.Mappers;
using ECommerce.Application.Features.Order.Rules;
using ECommerce.Application.Services.Repositories;
using ECommerce.Application.Features.Order.Constants;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.Order.Commands.Update
{
    // Sipariş bulunamazsa NotFoundException fırlar.
    // TotalPrice burada güncellenmez; kalemlerden hesaplanan bir değerdir (OrderItem komutları yönetir).
    public class UpdateOrderCommand : IRequest, ISecuredRequest
    {
        // Yetki: Order.Admin, Order.Write veya Order.Update claim'lerinden biri yeterli (global Admin her zaman geçer).
        // Açık (explicit) implementasyon: public property olmaz, JSON gövdesine / Swagger'a girmez.
        string[] ISecuredRequest.Roles => [OrderOperationClaims.Admin, OrderOperationClaims.Write, OrderOperationClaims.Update];

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
                OrderMapper.MapUpdateCommandToOrder(request, order);

                await orderRepository.UpdateAsync(order, cancellationToken);
            }
        }
    }
}
