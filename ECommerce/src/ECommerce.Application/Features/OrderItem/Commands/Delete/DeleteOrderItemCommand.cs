using ECommerce.Application.Features.OrderItem.Rules;
using ECommerce.Application.Services.OrderService;
using ECommerce.Application.Services.ProductService;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.OrderItem.Commands.Delete
{
    // Kalem bulunamazsa NotFoundException fırlar.
    // Kalemin adedi kadar ürün stoğu iade edilir, tutarı sipariş toplamından düşülür.
    public class DeleteOrderItemCommand : IRequest
    {
        public Guid Id { get; set; }

        public class DeleteOrderItemCommandHandler : IRequestHandler<DeleteOrderItemCommand>
        {
            // Dependency Injection
            // Kendi entity'si -> repository. Başka entity'ler (Order, Product) -> servis.
            private readonly IOrderItemRepository orderItemRepository;
            private readonly IOrderService orderService;
            private readonly IProductService productService;
            private readonly OrderItemBusinessRules orderItemBusinessRules;
            public DeleteOrderItemCommandHandler(
                IOrderItemRepository orderItemRepository,
                IOrderService orderService,
                IProductService productService,
                OrderItemBusinessRules orderItemBusinessRules)
            {
                this.orderItemRepository = orderItemRepository;
                this.orderService = orderService;
                this.productService = productService;
                this.orderItemBusinessRules = orderItemBusinessRules;
            }

            public async Task Handle(DeleteOrderItemCommand request, CancellationToken cancellationToken)
            {
                Domain.Entities.OrderItem? orderItem = await orderItemRepository.GetByIdAsync(request.Id, cancellationToken);
                orderItemBusinessRules.OrderItemShouldExistWhenSelected(orderItem);

                await orderItemRepository.DeleteAsync(orderItem, cancellationToken);

                // Stoğu iade et (ürün bu arada silinmişse servis hiçbir şey yapmaz).
                await productService.IncreaseStockIfProductExistsAsync(orderItem.ProductId, orderItem.Amount, cancellationToken);

                // Sipariş toplamından kalemin tutarını düş (sipariş yoksa servis hiçbir şey yapmaz).
                await orderService.ChangeTotalPriceIfOrderExistsAsync(orderItem.OrderId, -orderItem.TotalPrice.Amount, cancellationToken);
            }
        }
    }
}
