using ECommerce.Application.Features.OrderItem.Mappers;
using ECommerce.Application.Features.OrderItem.Rules;
using ECommerce.Application.Services.OrderService;
using ECommerce.Application.Services.ProductService;
using ECommerce.Application.Services.Repositories;
using ECommerce.Application.Features.OrderItem.Constants;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.OrderItem.Commands.Create
{
    // Var olan bir siparişe tek kalem ekler.
    // Sipariş ya da ürün bulunamazsa NotFoundException, kural ihlalinde BusinessException fırlar.
    // Fiyat istemciden alınmaz; ürünün güncel fiyatından hesaplanır (istemci fiyatı manipüle edemesin).
    // Kalemin adedi kadar ürün stoğundan düşülür; stok yetmezse kalem eklenmez.
    public class CreateOrderItemCommand : IRequest, ISecuredRequest
    {
        // Yetki: OrderItem.Admin, OrderItem.Write veya OrderItem.Create claim'lerinden biri yeterli (global Admin her zaman geçer).
        // Açık (explicit) implementasyon: public property olmaz, JSON gövdesine / Swagger'a girmez.
        string[] ISecuredRequest.Roles => [OrderItemOperationClaims.Admin, OrderItemOperationClaims.Write, OrderItemOperationClaims.Create];

        public Guid OrderId { get; set; }
        public Guid ProductId { get; set; }
        public int Amount { get; set; } // adet

        public class CreateOrderItemCommandHandler : IRequestHandler<CreateOrderItemCommand>
        {
            // Dependency Injection
            // Kendi entity'si -> repository. Başka entity'ler (Order, Product) -> servis.
            private readonly IOrderItemRepository orderItemRepository;
            private readonly IOrderService orderService;
            private readonly IProductService productService;
            private readonly OrderItemBusinessRules orderItemBusinessRules;
            public CreateOrderItemCommandHandler(
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

            public async Task Handle(CreateOrderItemCommand request, CancellationToken cancellationToken)
            {
                // Kurallar en üstte: bir kural eziliyorsa hiçbir şey değişmez.
                orderItemBusinessRules.AmountShouldBeGreaterThanZero(request.Amount);

                // Sipariş / ürün yoksa servisler NotFoundException fırlatır.
                Domain.Entities.Order order = await orderService.GetByIdAsync(request.OrderId, cancellationToken);
                Domain.Entities.Product product = await productService.GetByIdAsync(request.ProductId, cancellationToken);

                orderItemBusinessRules.ProductCurrencyShouldMatchOrderCurrency(product, order.TotalPrice.Currency);
                productService.StockShouldBeEnough(product, request.Amount);

                // Mapping
                Domain.Entities.OrderItem orderItem = OrderItemMapper.MapCreateCommandToOrderItem(request, product);

                await productService.DecreaseStockAsync(product, request.Amount, cancellationToken);
                await orderItemRepository.AddAsync(orderItem, cancellationToken);

                // Sipariş toplamına kalemin tutarını ekle.
                await orderService.ChangeTotalPriceAsync(order, orderItem.TotalPrice.Amount, cancellationToken);
            }
        }
    }
}
