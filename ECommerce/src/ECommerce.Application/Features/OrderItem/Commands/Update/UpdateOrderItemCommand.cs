using ECommerce.Application.Features.OrderItem.Mappers;
using ECommerce.Application.Features.OrderItem.Rules;
using ECommerce.Application.Services.OrderService;
using ECommerce.Application.Services.ProductService;
using ECommerce.Application.Services.Repositories;
using ECommerce.Application.Features.OrderItem.Constants;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.OrderItem.Commands.Update
{
    // Kalem, siparişi ya da ürünü bulunamazsa NotFoundException fırlar.
    // Yalnızca adet değişir. Ürün değiştirmek için kalem silinip yenisi eklenmeli.
    // Birim fiyat, kalem eklendiği andaki fiyattır (ürün fiyatı sonradan değişse de sipariş etkilenmez).
    // Stok, adet farkı kadar düşülür (artışta) ya da iade edilir (azalışta).
    public class UpdateOrderItemCommand : IRequest, ISecuredRequest
    {
        // Yetki: OrderItem.Admin, OrderItem.Write veya OrderItem.Update claim'lerinden biri yeterli (global Admin her zaman geçer).
        // Açık (explicit) implementasyon: public property olmaz, JSON gövdesine / Swagger'a girmez.
        string[] ISecuredRequest.Roles => [OrderItemOperationClaims.Admin, OrderItemOperationClaims.Write, OrderItemOperationClaims.Update];

        public Guid Id { get; set; }
        public int Amount { get; set; } // adet

        public class UpdateOrderItemCommandHandler : IRequestHandler<UpdateOrderItemCommand>
        {
            // Dependency Injection
            // Kendi entity'si -> repository. Başka entity'ler (Order, Product) -> servis.
            private readonly IOrderItemRepository orderItemRepository;
            private readonly IOrderService orderService;
            private readonly IProductService productService;
            private readonly OrderItemBusinessRules orderItemBusinessRules;
            public UpdateOrderItemCommandHandler(
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

            public async Task Handle(UpdateOrderItemCommand request, CancellationToken cancellationToken)
            {
                // Kurallar en üstte: bir kural eziliyorsa hiçbir şey değişmez.
                orderItemBusinessRules.AmountShouldBeGreaterThanZero(request.Amount);

                Domain.Entities.OrderItem? orderItem = await orderItemRepository.GetByIdAsync(request.Id, cancellationToken);
                orderItemBusinessRules.OrderItemShouldExistWhenSelected(orderItem);

                // Sipariş / ürün yoksa servisler NotFoundException fırlatır.
                Domain.Entities.Order order = await orderService.GetByIdAsync(orderItem.OrderId, cancellationToken);
                Domain.Entities.Product product = await productService.GetByIdAsync(orderItem.ProductId, cancellationToken);

                int amountDifference = request.Amount - orderItem.Amount;

                // Adet artıyorsa artış kadar stok gerekir (azalışta kural her zaman sağlanır).
                productService.StockShouldBeEnough(product, amountDifference);

                decimal oldTotal = orderItem.TotalPrice.Amount;

                // Stok farkı: artışta düşer, azalışta iade edilir (yön kararı servisin işi).
                await productService.ChangeStockByAmountDifferenceAsync(product, amountDifference, cancellationToken);

                // Mapping
                OrderItemMapper.MapUpdateCommandToOrderItem(request, orderItem);

                await orderItemRepository.UpdateAsync(orderItem, cancellationToken);

                // Sipariş toplamını farka göre düzelt.
                await orderService.ChangeTotalPriceAsync(order, orderItem.TotalPrice.Amount - oldTotal, cancellationToken);
            }
        }
    }
}
