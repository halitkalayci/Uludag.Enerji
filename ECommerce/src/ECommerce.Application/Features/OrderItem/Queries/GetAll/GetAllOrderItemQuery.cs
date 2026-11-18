using ECommerce.Application.Features.OrderItem.Mappers;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.OrderItem.Queries.GetAll
{
    // Gövdesi boş sorgu: tüm sipariş kalemlerini ister.
    public class GetAllOrderItemQuery : IRequest<List<OrderItemResponse>>
    {
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
