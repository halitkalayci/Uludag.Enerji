using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.Order.Queries.GetById
{
    // Sorgu -> geriye değer döndüren istek. Sipariş yoksa null döner.
    public class GetByIdOrderQuery : IRequest<OrderResponse?>
    {
        public Guid Id { get; set; }

        public class GetByIdOrderQueryHandler : IRequestHandler<GetByIdOrderQuery, OrderResponse?>
        {
            // Dependency Injection
            private readonly IOrderRepository orderRepository;
            public GetByIdOrderQueryHandler(IOrderRepository orderRepository)
            {
                this.orderRepository = orderRepository;
            }

            public async Task<OrderResponse?> Handle(GetByIdOrderQuery request, CancellationToken cancellationToken)
            {
                Domain.Entities.Order? order = await orderRepository.GetByIdAsync(request.Id, cancellationToken);

                if (order is null)
                    return null;

                // Mapping
                return new OrderResponse
                {
                    Id = order.Id,
                    OrderDate = order.OrderDate,
                    TotalPrice = order.TotalPrice.Amount,
                    Currency = order.TotalPrice.Currency
                };
            }
        }
    }
}
