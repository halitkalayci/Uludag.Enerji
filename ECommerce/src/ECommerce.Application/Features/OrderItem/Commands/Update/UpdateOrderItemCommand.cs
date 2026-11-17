using ECommerce.Application.Services.OrderService;
using ECommerce.Application.Services.ProductService;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.OrderItem.Commands.Update
{
    // Geriye bool döner: true -> güncellendi, false -> kalem, siparişi ya da ürünü bulunamadı.
    // Yalnızca adet değişir. Ürün değiştirmek için kalem silinip yenisi eklenmeli.
    // Birim fiyat, kalem eklendiği andaki fiyattır (ürün fiyatı sonradan değişse de sipariş etkilenmez).
    // Stok, adet farkı kadar düşülür (artışta) ya da iade edilir (azalışta).
    public class UpdateOrderItemCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
        public int Amount { get; set; } // adet

        public class UpdateOrderItemCommandHandler : IRequestHandler<UpdateOrderItemCommand, bool>
        {
            // Dependency Injection
            // Kendi entity'si -> repository. Başka entity'ler (Order, Product) -> servis.
            private readonly IOrderItemRepository orderItemRepository;
            private readonly IOrderService orderService;
            private readonly IProductService productService;
            public UpdateOrderItemCommandHandler(
                IOrderItemRepository orderItemRepository,
                IOrderService orderService,
                IProductService productService)
            {
                this.orderItemRepository = orderItemRepository;
                this.orderService = orderService;
                this.productService = productService;
            }

            public async Task<bool> Handle(UpdateOrderItemCommand request, CancellationToken cancellationToken)
            {
                if (request.Amount <= 0)
                    throw new ArgumentException("Amount must be greater than zero.");

                Domain.Entities.OrderItem? orderItem = await orderItemRepository.GetByIdAsync(request.Id, cancellationToken);
                if (orderItem is null)
                    return false;

                Domain.Entities.Order? order = await orderService.GetByIdAsync(orderItem.OrderId, cancellationToken);
                if (order is null)
                    return false;

                Domain.Entities.Product? product = await productService.GetByIdAsync(orderItem.ProductId, cancellationToken);
                if (product is null)
                    return false;

                int amountDifference = request.Amount - orderItem.Amount;

                if (amountDifference > 0 && !productService.HasEnoughStock(product, amountDifference))
                    throw new ArgumentException($"Insufficient stock for product {product.Id}.");

                decimal unitPrice = orderItem.TotalPrice.Amount / orderItem.Amount;
                decimal oldTotal = orderItem.TotalPrice.Amount;

                // Stok farkı: artışta düş, azalışta iade et.
                if (amountDifference > 0)
                    await productService.DecreaseStockAsync(product, amountDifference, cancellationToken);
                else if (amountDifference < 0)
                    await productService.IncreaseStockAsync(product, -amountDifference, cancellationToken);

                // Mapping
                orderItem.Amount = request.Amount;
                orderItem.TotalPrice = new Domain.ValueObjects.Money(unitPrice * request.Amount, orderItem.TotalPrice.Currency);

                await orderItemRepository.UpdateAsync(orderItem, cancellationToken);

                // Sipariş toplamını farka göre düzelt.
                await orderService.ChangeTotalPriceAsync(order, orderItem.TotalPrice.Amount - oldTotal, cancellationToken);
                return true;
            }
        }
    }
}
