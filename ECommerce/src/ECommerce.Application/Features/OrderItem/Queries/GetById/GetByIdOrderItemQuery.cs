using ECommerce.Application.Features.OrderItem.Mappers;
using ECommerce.Application.Features.OrderItem.Rules;
using ECommerce.Application.Services.Repositories;
using ECommerce.Application.Features.OrderItem.Constants;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.OrderItem.Queries.GetById
{
    // Sorgu -> geriye değer döndüren istek. Kalem yoksa NotFoundException fırlar (null dönmez).
    public class GetByIdOrderItemQuery : IRequest<OrderItemResponse>, ISecuredRequest
    {
        // Yetki: OrderItem.Admin veya OrderItem.Read claim'lerinden biri yeterli (global Admin her zaman geçer).
        // Açık (explicit) implementasyon: public property olmaz, JSON gövdesine / Swagger'a girmez.
        string[] ISecuredRequest.Roles => [OrderItemOperationClaims.Admin, OrderItemOperationClaims.Read];

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
                return OrderItemMapper.MapOrderItemToResponse(orderItem);
            }
        }
    }
}
