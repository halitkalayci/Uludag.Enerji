using ECommerce.Application.Features.Auth.Rules;
using ECommerce.Application.Features.Order.Mappers;
using ECommerce.Application.Features.Order.Rules;
using ECommerce.Application.Features.OrderItem.Rules;
using ECommerce.Application.Services.PaymentService;
using ECommerce.Application.Services.ProductService;
using ECommerce.Application.Services.Repositories;
using ECommerce.Application.Features.Order.Constants;
using Uludag.Core.Mediation;
using Uludag.Core.Security.JWT;

namespace ECommerce.Application.Features.Order.Commands.Create
{
    // Siparişi kalemleriyle birlikte tek seferde (tek SaveChanges) oluşturur.
    // Kalemlerdeki ürünlerden biri bulunamazsa NotFoundException, kural ihlalinde BusinessException fırlar.
    // OrderDate, fiyatlar ve siparişin sahibi (UserId) istemciden alınmaz, sunucu belirler.
    // Sahip, isteğin Bearer token'ının oturumundan alınır (başkası adına sipariş verilemez).
    // Her kalemin adedi kadar ürün stoğundan düşülür; stok yetmezse sipariş oluşmaz.
    // Doğrulamalardan sonra siparişin toplamı için ödeme denenir (spec 0008, BR-12); ödeme reddedilirse
    // stok düşülmez ve sipariş kaydedilmez — ödeme bu yüzden stok değişikliğinden ÖNCE yapılır.
    public class CreateOrderCommand : IRequest
    {
        // Yetki: Order.Admin, Order.Write veya Order.Create claim'lerinden biri yeterli (global Admin her zaman geçer).
        // Açık (explicit) implementasyon: public property olmaz, JSON gövdesine / Swagger'a girmez.
        

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
            private readonly IPaymentService paymentService; // dış sağlayıcı (Infrastructure) -> arayüz üzerinden.
            private readonly OrderBusinessRules orderBusinessRules;
            private readonly OrderItemBusinessRules orderItemBusinessRules;
            private readonly AuthBusinessRules authBusinessRules;
            private readonly ITokenSessionAccessor tokenSessionAccessor; // "kim giriş yapmış" -> siparişin sahibi.
            public CreateOrderCommandHandler(
                IOrderRepository orderRepository,
                IProductService productService,
                IPaymentService paymentService,
                OrderBusinessRules orderBusinessRules,
                OrderItemBusinessRules orderItemBusinessRules,
                AuthBusinessRules authBusinessRules,
                ITokenSessionAccessor tokenSessionAccessor)
            {
                this.orderRepository = orderRepository;
                this.productService = productService;
                this.paymentService = paymentService;
                this.orderBusinessRules = orderBusinessRules;
                this.orderItemBusinessRules = orderItemBusinessRules;
                this.authBusinessRules = authBusinessRules;
                this.tokenSessionAccessor = tokenSessionAccessor;
            }

            // Orkestrasyon yapıyor mu?
            // İş kuralı içeri sızmış mı?
            // Mapper çağırıyor mu?
            public async Task Handle(CreateOrderCommand request, CancellationToken cancellationToken)
            {
                // Siparişin sahibi: isteği yapan kullanıcı. Stok düşülmeden önce kontrol edilir.
                TokenSession? tokenSession = await tokenSessionAccessor.GetCurrentTokenSessionAsync();
                authBusinessRules.TokenSessionShouldExist(tokenSession);

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

                // IDOR - Broken Access Control
                // Devre dışı 
                // UserId gibi kimliğe bağlı alanlar -> Asla parametre değil tamamen oturumdan sunucu tarafında atanmalı.
                // Mapper toplamı sunucudaki fiyatlardan hesaplar; ödenecek tutar budur (istemciden tutar alınmaz).
                Domain.Entities.Order order = OrderMapper.MapCreateCommandToOrder(request, products, tokenSession.UserId);

                // Ödeme: stok düşülmeden ve sipariş kaydedilmeden önce. Ret -> BusinessException, hiçbir şey değişmez.
                PaymentResult paymentResult = await paymentService.PayAsync(new PaymentRequest(tokenSession.UserId, order.TotalPrice), cancellationToken);
                orderBusinessRules.PaymentShouldBeApproved(paymentResult);

                foreach ((Guid productId, int amount) in requestedAmounts)
                    await productService.DecreaseStockAsync(products[productId], amount, cancellationToken);

                await orderRepository.AddAsync(order, cancellationToken);
            }
        }
    }
}
