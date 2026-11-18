using ECommerce.Application.Features.Order.Rules;
using ECommerce.Application.Services.ProductService;
using ECommerce.Application.Services.Repositories;
using ECommerce.Application.Features.Order.Constants;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.Order.Commands.Delete
{
    // Sipariş bulunamazsa NotFoundException fırlar.
    // Sipariş silinince kalemleri de silinir ve her kalemin adedi kadar ürün stoğu iade edilir.
    public class DeleteOrderCommand : IRequest, ISecuredRequest
    {
        // Yetki: Order.Admin, Order.Write veya Order.Delete claim'lerinden biri yeterli (global Admin her zaman geçer).
        // Açık (explicit) implementasyon: public property olmaz, JSON gövdesine / Swagger'a girmez.
        string[] ISecuredRequest.Roles => [OrderOperationClaims.Admin, OrderOperationClaims.Write, OrderOperationClaims.Delete];

        public Guid Id { get; set; }

        public class DeleteOrderCommandHandler : IRequestHandler<DeleteOrderCommand>
        {
            // Dependency Injection
            // Kendi entity'si -> repository. Başka entity (Product) -> servis.
            private readonly IOrderRepository orderRepository;
            private readonly IProductService productService;
            private readonly OrderBusinessRules orderBusinessRules;
            public DeleteOrderCommandHandler(
                IOrderRepository orderRepository,
                IProductService productService,
                OrderBusinessRules orderBusinessRules)
            {
                this.orderRepository = orderRepository;
                this.productService = productService;
                this.orderBusinessRules = orderBusinessRules;
            }

            public async Task Handle(DeleteOrderCommand request, CancellationToken cancellationToken)
            {
                Domain.Entities.Order? order = await orderRepository.GetByIdWithItemsAsync(request.Id, cancellationToken);
                orderBusinessRules.OrderShouldExistWhenSelected(order);

                // Silmeden önce iade edilecek adetleri ürün bazında topla
                // (silme sonrası Items koleksiyonu EF tarafından boşaltılır).
                Dictionary<Guid, int> returnedAmounts = order.Items
                    .GroupBy(i => i.ProductId)
                    .ToDictionary(g => g.Key, g => g.Sum(i => i.Amount));

                await orderRepository.DeleteAsync(order, cancellationToken);

                // Stoğu iade et (ürün bu arada silinmişse servis hiçbir şey yapmaz).
                foreach ((Guid productId, int amount) in returnedAmounts)
                    await productService.IncreaseStockIfProductExistsAsync(productId, amount, cancellationToken);
            }
        }
    }
}
