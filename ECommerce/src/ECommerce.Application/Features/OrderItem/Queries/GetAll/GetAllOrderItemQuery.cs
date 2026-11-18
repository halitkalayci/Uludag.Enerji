using ECommerce.Application.Features.OrderItem.Mappers;
using ECommerce.Application.Services.Repositories;
using ECommerce.Application.Features.OrderItem.Constants;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.OrderItem.Queries.GetAll
{
    // Gövdesi boş sorgu: tüm sipariş kalemlerini ister.
    public class GetAllOrderItemQuery : IRequest<List<OrderItemResponse>>, ISecuredRequest
    {
        // Yetki: OrderItem.Admin veya OrderItem.Read claim'lerinden biri yeterli (global Admin her zaman geçer).
        // Açık (explicit) implementasyon: public property olmaz, JSON gövdesine / Swagger'a girmez.
        string[] ISecuredRequest.Roles => [OrderItemOperationClaims.Admin, OrderItemOperationClaims.Read];

        public class GetAllOrderItemQueryHandler : IRequestHandler<GetAllOrderItemQuery, List<OrderItemResponse>>
        {
            // Dependency Injection
            private readonly IOrderItemRepository orderItemRepository;
            public GetAllOrderItemQueryHandler(IOrderItemRepository orderItemRepository)
            {
                this.orderItemRepository = orderItemRepository;
            }

            public async Task<List<OrderItemResponse>> Handle(GetAllOrderItemQuery request, CancellationToken cancellationToken)
            {
                List<Domain.Entities.OrderItem> orderItems = await orderItemRepository.GetAllAsync(cancellationToken);

                // Mapping
                return OrderItemMapper.MapOrderItemsToResponses(orderItems);
            }
        }
    }
}
