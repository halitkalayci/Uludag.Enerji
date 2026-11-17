using ECommerce.Application.Services.ProductService;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.Order.Commands.Create
{
    // Siparişi kalemleriyle birlikte tek seferde (tek SaveChanges) oluşturur.
    // Geriye bool döner: true -> oluşturuldu, false -> kalemlerdeki ürünlerden biri bulunamadı.
    // OrderDate ve fiyatlar istemciden alınmaz, sunucu belirler.
    // NOT: Ürün stoğu henüz düşülmüyor (bilinen risk, sonra ele alınacak).
    public class CreateOrderCommand : IRequest<bool>
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

        public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, bool>
        {
            // Dependency Injection
            // Kendi entity'si -> repository. Başka entity (Product) -> servis.
            // OrderItem için ayrı repository gerekmez: kalemler Order aggregate'inin parçası olarak kaydedilir.
            private readonly IOrderRepository orderRepository;
            private readonly IProductService productService;
            public CreateOrderCommandHandler(IOrderRepository orderRepository, IProductService productService)
            {
                this.orderRepository = orderRepository;
                this.productService = productService;
            }

            public async Task<bool> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
            {
                if (request.Items is null || request.Items.Count == 0)
                    throw new ArgumentException("Order must contain at least one item.");

                Domain.Entities.Order order = new()
                {
                    OrderDate = DateTime.UtcNow
                };

                decimal totalPrice = 0;
                foreach (CreateOrderItemDto item in request.Items)
                {
                    if (item.Amount <= 0)
                        throw new ArgumentException("Amount must be greater than zero.");

                    Domain.Entities.Product? product = await productService.GetByIdAsync(item.ProductId, cancellationToken);
                    if (product is null)
                        return false;

                    if (product.Price.Currency != request.Currency)
                        throw new ArgumentException("Product currency does not match the order currency.");

                    // Mapping (her entity kendi Money örneğini almalı; product.Price paylaşılmaz)
                    Domain.Entities.OrderItem orderItem = new()
                    {
                        ProductId = product.Id,
                        Amount = item.Amount,
                        TotalPrice = new Domain.ValueObjects.Money(product.Price.Amount * item.Amount, product.Price.Currency)
                    };

                    order.Items.Add(orderItem); // OrderId, EF tarafından navigation üzerinden atanır.
                    totalPrice += orderItem.TotalPrice.Amount;
                }

                order.TotalPrice = new Domain.ValueObjects.Money(totalPrice, request.Currency);

                // Sipariş + tüm kalemleri tek SaveChanges ile kaydedilir (ya hepsi ya hiçbiri).
                await orderRepository.AddAsync(order, cancellationToken);
                return true;
            }
        }
    }
}
