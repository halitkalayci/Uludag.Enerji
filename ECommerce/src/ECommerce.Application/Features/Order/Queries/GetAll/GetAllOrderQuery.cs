using ECommerce.Application.Features.Order.Mappers;
using ECommerce.Application.Services.Repositories;
using ECommerce.Application.Features.Order.Constants;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.Order.Queries.GetAll
{
    // Gövdesi boş sorgu: tüm siparişleri ister.
    public class GetAllOrderQuery : IRequest<List<OrderResponse>>, ISecuredRequest
    {
        // Yetki: Order.Admin veya Order.Read claim'lerinden biri yeterli (global Admin her zaman geçer).
        // Açık (explicit) implementasyon: public property olmaz, JSON gövdesine / Swagger'a girmez.
        string[] ISecuredRequest.Roles => [OrderOperationClaims.Admin, OrderOperationClaims.Read];

        public class GetAllOrderQueryHandler : IRequestHandler<GetAllOrderQuery, List<OrderResponse>>
        {
            // Dependency Injection
            private readonly IOrderRepository orderRepository;
            public GetAllOrderQueryHandler(IOrderRepository orderRepository)
            {
                this.orderRepository = orderRepository;
            }

            public async Task<List<OrderResponse>> Handle(GetAllOrderQuery request, CancellationToken cancellationToken)
            {
                List<Domain.Entities.Order> orders = await orderRepository.GetAllAsync(cancellationToken);

                // Mapping
                return OrderMapper.MapOrdersToResponses(orders);
            }
        }
    }
}
