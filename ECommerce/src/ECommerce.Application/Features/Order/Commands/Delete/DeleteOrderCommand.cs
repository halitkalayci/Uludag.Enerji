using ECommerce.Application.Services.ProductService;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.Order.Commands.Delete
{
    // Geriye bool döner: true -> silindi, false -> sipariş bulunamadı.
    // Sipariş silinince kalemleri de silinir ve her kalemin adedi kadar ürün stoğu iade edilir.
    public class DeleteOrderCommand : IRequest<bool>
    {
        public Guid Id { get; set; }

        public class DeleteOrderCommandHandler : IRequestHandler<DeleteOrderCommand, bool>
        {
            // Dependency Injection
            // Kendi entity'si -> repository. Başka entity (Product) -> servis.
            private readonly IOrderRepository orderRepository;
            private readonly IProductService productService;
            public DeleteOrderCommandHandler(IOrderRepository orderRepository, IProductService productService)
            {
                this.orderRepository = orderRepository;
                this.productService = productService;
            }

            public async Task<bool> Handle(DeleteOrderCommand request, CancellationToken cancellationToken)
            {
                Domain.Entities.Order? order = await orderRepository.GetByIdWithItemsAsync(request.Id, cancellationToken);

                if (order is null)
                    return false;

                // Silmeden önce iade edilecek adetleri ürün bazında topla
                // (silme sonrası Items koleksiyonu EF tarafından boşaltılır).
                Dictionary<Guid, int> returnedAmounts = order.Items
                    .GroupBy(i => i.ProductId)
                    .ToDictionary(g => g.Key, g => g.Sum(i => i.Amount));

                await orderRepository.DeleteAsync(order, cancellationToken);

                // Stoğu iade et (ürün bu arada silinmişse iade edilecek stok yoktur).
                foreach ((Guid productId, int amount) in returnedAmounts)
                {
                    Domain.Entities.Product? product = await productService.GetByIdAsync(productId, cancellationToken);
                    if (product is not null)
                        await productService.IncreaseStockAsync(product, amount, cancellationToken);
                }

                return true;
            }
        }
    }
}
