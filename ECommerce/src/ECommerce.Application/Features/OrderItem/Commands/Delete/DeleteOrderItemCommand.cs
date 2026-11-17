using ECommerce.Application.Services.OrderService;
using ECommerce.Application.Services.ProductService;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.OrderItem.Commands.Delete
{
    // Geriye bool döner: true -> silindi, false -> kalem bulunamadı.
    // Kalemin adedi kadar ürün stoğu iade edilir.
    public class DeleteOrderItemCommand : IRequest<bool>
    {
        public Guid Id { get; set; }

        public class DeleteOrderItemCommandHandler : IRequestHandler<DeleteOrderItemCommand, bool>
        {
            // Dependency Injection
            // Kendi entity'si -> repository. Başka entity'ler (Order, Product) -> servis.
            private readonly IOrderItemRepository orderItemRepository;
            private readonly IOrderService orderService;
            private readonly IProductService productService;
            public DeleteOrderItemCommandHandler(
                IOrderItemRepository orderItemRepository,
                IOrderService orderService,
                IProductService productService)
            {
                this.orderItemRepository = orderItemRepository;
                this.orderService = orderService;
                this.productService = productService;
            }

            public async Task<bool> Handle(DeleteOrderItemCommand request, CancellationToken cancellationToken)
            {
                Domain.Entities.OrderItem? orderItem = await orderItemRepository.GetByIdAsync(request.Id, cancellationToken);
                if (orderItem is null)
                    return false;

                Domain.Entities.Order? order = await orderService.GetByIdAsync(orderItem.OrderId, cancellationToken);
                Domain.Entities.Product? product = await productService.GetByIdAsync(orderItem.ProductId, cancellationToken);

                await orderItemRepository.DeleteAsync(orderItem, cancellationToken);

                // Stoğu iade et (ürün bu arada silinmişse iade edilecek stok yoktur).
                if (product is not null)
                    await productService.IncreaseStockAsync(product, orderItem.Amount, cancellationToken);

                // Sipariş toplamından kalemin tutarını düş.
                if (order is not null)
                    await orderService.ChangeTotalPriceAsync(order, -orderItem.TotalPrice.Amount, cancellationToken);

                return true;
            }
        }
    }
}
