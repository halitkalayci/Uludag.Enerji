using ECommerce.Application.Services.OrderService;
using ECommerce.Application.Services.ProductService;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.OrderItem.Commands.Create
{
    // Var olan bir siparişe tek kalem ekler.
    // Geriye bool döner: true -> eklendi, false -> sipariş ya da ürün bulunamadı.
    // Fiyat istemciden alınmaz; ürünün güncel fiyatından hesaplanır (istemci fiyatı manipüle edemesin).
    // NOT: Ürün stoğu henüz düşülmüyor (bilinen risk, sonra ele alınacak).
    public class CreateOrderItemCommand : IRequest<bool>
    {
        public Guid OrderId { get; set; }
        public Guid ProductId { get; set; }
        public int Amount { get; set; } // adet

        public class CreateOrderItemCommandHandler : IRequestHandler<CreateOrderItemCommand, bool>
        {
            // Dependency Injection
            // Kendi entity'si -> repository. Başka entity'ler (Order, Product) -> servis.
            private readonly IOrderItemRepository orderItemRepository;
            private readonly IOrderService orderService;
            private readonly IProductService productService;
            public CreateOrderItemCommandHandler(
                IOrderItemRepository orderItemRepository,
                IOrderService orderService,
                IProductService productService)
            {
                this.orderItemRepository = orderItemRepository;
                this.orderService = orderService;
                this.productService = productService;
            }

            public async Task<bool> Handle(CreateOrderItemCommand request, CancellationToken cancellationToken)
            {
                if (request.Amount <= 0)
                    throw new ArgumentException("Amount must be greater than zero.");

                Domain.Entities.Order? order = await orderService.GetByIdAsync(request.OrderId, cancellationToken);
                if (order is null)
                    return false;

                Domain.Entities.Product? product = await productService.GetByIdAsync(request.ProductId, cancellationToken);
                if (product is null)
                    return false;

                if (product.Price.Currency != order.TotalPrice.Currency)
                    throw new ArgumentException("Product currency does not match the order currency.");

                // Mapping (her entity kendi Money örneğini almalı; product.Price paylaşılmaz)
                Domain.Entities.OrderItem orderItem = new()
                {
                    OrderId = order.Id,
                    ProductId = product.Id,
                    Amount = request.Amount,
                    TotalPrice = new Domain.ValueObjects.Money(product.Price.Amount * request.Amount, product.Price.Currency)
                };

                await orderItemRepository.AddAsync(orderItem, cancellationToken);

                // Sipariş toplamına kalemin tutarını ekle.
                await orderService.ChangeTotalPriceAsync(order, orderItem.TotalPrice.Amount, cancellationToken);
                return true;
            }
        }
    }
}
