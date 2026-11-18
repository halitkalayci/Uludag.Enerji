using ECommerce.Application.Features.Order.Rules;
using ECommerce.Application.Features.OrderItem.Rules;
using ECommerce.Application.Services.ProductService;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.Order.Commands.Create
{
    // Siparişi kalemleriyle birlikte tek seferde (tek SaveChanges) oluşturur.
    // Kalemlerdeki ürünlerden biri bulunamazsa NotFoundException, kural ihlalinde BusinessException fırlar.
    // OrderDate ve fiyatlar istemciden alınmaz, sunucu belirler.
    // Her kalemin adedi kadar ürün stoğundan düşülür; stok yetmezse sipariş oluşmaz.
    public class CreateOrderCommand : IRequest
    {
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

            public async Task Handle(CreateOrderCommand request, CancellationToken cancellationToken)
            {
                // 1) Kurallar: hiçbir şeye dokunmadan önce tüm kalemleri kontrol et.
                // En üstte. Bir kural eziliyorsa zaten işlem yapılmayacaktır.
                orderBusinessRules.OrderShouldHaveAtLeastOneItem(request.Items);

                foreach (CreateOrderItemDto item in request.Items)
                    orderItemBusinessRules.AmountShouldBeGreaterThanZero(item.Amount);

                // Aynı ürün birden fazla satırda gelebilir; stok kontrolü ürün bazında toplam adetle yapılır.
                Dictionary<Guid, int> requestedAmounts = request.Items
                    .GroupBy(i => i.ProductId)
                    .ToDictionary(g => g.Key, g => g.Sum(i => i.Amount));

                Dictionary<Guid, Domain.Entities.Product> products = new();
                foreach ((Guid productId, int amount) in requestedAmounts)
                {
                    // Ürün yoksa servis NotFoundException fırlatır.
                    Domain.Entities.Product product = await productService.GetByIdAsync(productId, cancellationToken);

                    orderItemBusinessRules.ProductCurrencyShouldMatchOrderCurrency(product, request.Currency);
                    productService.StockShouldBeEnough(product, amount);

                    products[productId] = product;
                }

                // 2) Stokları düş (kurallar geçtiği için burada yetersiz stok hatası beklenmez).
                foreach ((Guid productId, int amount) in requestedAmounts)
                    await productService.DecreaseStockAsync(products[productId], amount, cancellationToken);

                // 3) Siparişi kalemleriyle oluştur.
                Domain.Entities.Order order = new()
                {
                    OrderDate = DateTime.UtcNow
                };

                foreach (CreateOrderItemDto item in request.Items)
                {
                    Domain.Entities.Product product = products[item.ProductId];

                    // Mapping (her entity kendi Money örneğini almalı; product.Price paylaşılmaz)
                    Domain.Entities.OrderItem orderItem = new()
                    {
                        ProductId = product.Id,
                        Amount = item.Amount,
                        TotalPrice = new Domain.ValueObjects.Money(product.Price.Amount * item.Amount, product.Price.Currency)
                    };

                    order.Items.Add(orderItem); // OrderId, EF tarafından navigation üzerinden atanır.
                }

                order.TotalPrice = new Domain.ValueObjects.Money(order.Items.Sum(i => i.TotalPrice.Amount), request.Currency);

                // Sipariş + tüm kalemleri tek SaveChanges ile kaydedilir (ya hepsi ya hiçbiri).
                await orderRepository.AddAsync(order, cancellationToken);
            }
        }
    }
}
