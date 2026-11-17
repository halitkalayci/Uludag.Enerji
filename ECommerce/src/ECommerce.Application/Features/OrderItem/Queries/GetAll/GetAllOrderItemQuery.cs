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
                return orderItems
                    .Select(i => new OrderItemResponse
                    {
                        Id = i.Id,
                        OrderId = i.OrderId,
                        ProductId = i.ProductId,
                        Amount = i.Amount,
                        TotalPrice = i.TotalPrice.Amount,
                        Currency = i.TotalPrice.Currency
                    })
                    .ToList();
            }
        }
    }
}
