using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.OrderItem.Queries.GetById
{
    // Sorgu -> geriye değer döndüren istek. Kalem yoksa null döner.
    public class GetByIdOrderItemQuery : IRequest<OrderItemResponse?>
    {
        public Guid Id { get; set; }

        public class GetByIdOrderItemQueryHandler : IRequestHandler<GetByIdOrderItemQuery, OrderItemResponse?>
        {
            // Dependency Injection
            private readonly IOrderItemRepository orderItemRepository;
            public GetByIdOrderItemQueryHandler(IOrderItemRepository orderItemRepository)
            {
                this.orderItemRepository = orderItemRepository;
            }

            public async Task<OrderItemResponse?> Handle(GetByIdOrderItemQuery request, CancellationToken cancellationToken)
            {
                Domain.Entities.OrderItem? orderItem = await orderItemRepository.GetByIdAsync(request.Id, cancellationToken);

                if (orderItem is null)
                    return null;

                // Mapping
                return new OrderItemResponse
                {
                    Id = orderItem.Id,
                    OrderId = orderItem.OrderId,
                    ProductId = orderItem.ProductId,
                    Amount = orderItem.Amount,
                    TotalPrice = orderItem.TotalPrice.Amount,
                    Currency = orderItem.TotalPrice.Currency
                };
            }
        }
    }
}
