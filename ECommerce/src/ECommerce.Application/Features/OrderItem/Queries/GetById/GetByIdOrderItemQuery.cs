using ECommerce.Application.Features.OrderItem.Rules;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.OrderItem.Queries.GetById
{
    // Sorgu -> geriye değer döndüren istek. Kalem yoksa NotFoundException fırlar (null dönmez).
    public class GetByIdOrderItemQuery : IRequest<OrderItemResponse>
    {
        public Guid Id { get; set; }

        public class GetByIdOrderItemQueryHandler : IRequestHandler<GetByIdOrderItemQuery, OrderItemResponse>
        {
            // Dependency Injection
            private readonly IOrderItemRepository orderItemRepository;
            private readonly OrderItemBusinessRules orderItemBusinessRules;
            public GetByIdOrderItemQueryHandler(IOrderItemRepository orderItemRepository, OrderItemBusinessRules orderItemBusinessRules)
            {
                this.orderItemRepository = orderItemRepository;
                this.orderItemBusinessRules = orderItemBusinessRules;
            }

            public async Task<OrderItemResponse> Handle(GetByIdOrderItemQuery request, CancellationToken cancellationToken)
            {
                Domain.Entities.OrderItem? orderItem = await orderItemRepository.GetByIdAsync(request.Id, cancellationToken);
                orderItemBusinessRules.OrderItemShouldExistWhenSelected(orderItem);

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
