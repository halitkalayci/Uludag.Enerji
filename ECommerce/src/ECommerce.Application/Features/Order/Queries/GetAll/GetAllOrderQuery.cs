using ECommerce.Application.Features.Order.Mappers;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.Order.Queries.GetAll
{
    // Gövdesi boş sorgu: tüm siparişleri ister.
    public class GetAllOrderQuery : IRequest<List<OrderResponse>>
    {
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
