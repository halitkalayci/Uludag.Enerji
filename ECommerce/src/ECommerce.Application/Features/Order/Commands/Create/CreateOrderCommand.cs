using ECommerce.Application.Features.Order.Mappers;
using ECommerce.Application.Features.Order.Rules;
using ECommerce.Application.Features.OrderItem.Rules;
using ECommerce.Application.Services.ProductService;
using ECommerce.Application.Services.Repositories;
using ECommerce.Application.Features.Order.Constants;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.Order.Commands.Create
{
    // Siparişi kalemleriyle birlikte tek seferde (tek SaveChanges) oluşturur.
    // Kalemlerdeki ürünlerden biri bulunamazsa NotFoundException, kural ihlalinde BusinessException fırlar.
    // OrderDate ve fiyatlar istemciden alınmaz, sunucu belirler.
    // Her kalemin adedi kadar ürün stoğundan düşülür; stok yetmezse sipariş oluşmaz.
    public class CreateOrderCommand : IRequest, ISecuredRequest
    {
        // Yetki: Order.Admin, Order.Write veya Order.Create claim'lerinden biri yeterli (global Admin her zaman geçer).
        // Açık (explicit) implementasyon: public property olmaz, JSON gövdesine / Swagger'a girmez.
        string[] ISecuredRequest.Roles => [OrderOperationClaims.Admin, OrderOperationClaims.Write, OrderOperationClaims.Create];

        // Siparişin para birimi. Kalemlerdeki tüm ürünler bu para biriminde olmalı.
        public string Currency { get; set; } = "TRY";
        public List<CreateOrderItemDto> Items { get; set; } = new();

        // Komut gövdesinin bir parçası: siparişe eklenecek kalem.
        public class CreateOrderItemDto
        {
            public Guid ProductId { get; set; }
            public int Amount { get; set; } // adet
        }

        public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand>
        {
            // Dependency Injection
            // Kendi entity'si -> repository. Başka entity (Product) -> servis.
            // OrderItem için ayrı repository gerekmez: kalemler Order aggregate'inin parçası olarak kaydedilir.
            // Kalemleri de bu komut oluşturduğu için OrderItem kuralları da burada geçerlidir.
            private readonly IOrderRepository orderRepository;
            private readonly IProductService productService;
            private readonly OrderBusinessRules orderBusinessRules;
            private readonly OrderItemBusinessRules orderItemBusinessRules;
            public CreateOrderCommandHandler(
                IOrderRepository orderRepository,
                IProductService productService,
                OrderBusinessRules orderBusinessRules,
                OrderItemBusinessRules orderItemBusinessRules)
            {
                this.orderRepository = orderRepository;
                this.productService = productService;
                this.orderBusinessRules = orderBusinessRules;
                this.orderItemBusinessRules = orderItemBusinessRules;
            }

            // Orkestrasyon yapıyor mu?
            // İş kuralı içeri sızmış mı?
            // Mapper çağırıyor mu?
            public async Task Handle(CreateOrderCommand request, CancellationToken cancellationToken)
            {
                orderBusinessRules.OrderShouldHaveAtLeastOneItem(request.Items); 

                foreach (CreateOrderItemDto item in request.Items)
                    orderItemBusinessRules.AmountShouldBeGreaterThanZero(item.Amount);

                Dictionary<Guid, int> requestedAmounts = request.Items
                    .GroupBy(i => i.ProductId)
                    .ToDictionary(g => g.Key, g => g.Sum(i => i.Amount));

                Dictionary<Guid, Domain.Entities.Product> products = new();
                foreach ((Guid productId, int amount) in requestedAmounts)
                {
                    Domain.Entities.Product product = await productService.GetByIdAsync(productId, cancellationToken);

                    orderItemBusinessRules.ProductCurrencyShouldMatchOrderCurrency(product, request.Currency);
                    productService.StockShouldBeEnough(product, amount);

                    products[productId] = product;
                }

                foreach ((Guid productId, int amount) in requestedAmounts)
                    await productService.DecreaseStockAsync(products[productId], amount, cancellationToken);

                Domain.Entities.Order order = OrderMapper.MapCreateCommandToOrder(request, products);

                await orderRepository.AddAsync(order, cancellationToken);
            }
        }
    }
}
