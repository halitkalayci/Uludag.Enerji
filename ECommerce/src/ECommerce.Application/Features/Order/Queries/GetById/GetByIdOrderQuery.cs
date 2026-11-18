using ECommerce.Application.Features.Order.Mappers;
using ECommerce.Application.Features.Order.Rules;
using ECommerce.Application.Services.Repositories;
using ECommerce.Application.Features.Order.Constants;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.Order.Queries.GetById
{
    // Sorgu -> geriye değer döndüren istek. Sipariş yoksa NotFoundException fırlar (null dönmez).
    public class GetByIdOrderQuery : IRequest<OrderResponse>, ISecuredRequest
    {
        // Yetki: Order.Admin veya Order.Read claim'lerinden biri yeterli (global Admin her zaman geçer).
        // Açık (explicit) implementasyon: public property olmaz, JSON gövdesine / Swagger'a girmez.
        string[] ISecuredRequest.Roles => [OrderOperationClaims.Admin, OrderOperationClaims.Read];

        public Guid Id { get; set; }

        public class GetByIdOrderQueryHandler : IRequestHandler<GetByIdOrderQuery, OrderResponse>
        {
            // Dependency Injection
            private readonly IOrderRepository orderRepository;
            private readonly OrderBusinessRules orderBusinessRules;
            public GetByIdOrderQueryHandler(IOrderRepository orderRepository, OrderBusinessRules orderBusinessRules)
            {
                this.orderRepository = orderRepository;
                this.orderBusinessRules = orderBusinessRules;
            }

            public async Task<OrderResponse> Handle(GetByIdOrderQuery request, CancellationToken cancellationToken)
            {
                Domain.Entities.Order? order = await orderRepository.GetByIdAsync(request.Id, cancellationToken);
                orderBusinessRules.OrderShouldExistWhenSelected(order);

                // Mapping
                return OrderMapper.MapOrderToResponse(order);
            }
        }
    }
}
