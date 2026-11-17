using ECommerce.Application.Services.OrderService;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.OrderItem.Commands.Update
{
    // Geriye bool döner: true -> güncellendi, false -> kalem ya da siparişi bulunamadı.
    // Yalnızca adet değişir. Ürün değiştirmek için kalem silinip yenisi eklenmeli.
    // Birim fiyat, kalem eklendiği andaki fiyattır (ürün fiyatı sonradan değişse de sipariş etkilenmez).
    public class UpdateOrderItemCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
        public int Amount { get; set; } // adet

        public class UpdateOrderItemCommandHandler : IRequestHandler<UpdateOrderItemCommand, bool>
        {
            // Dependency Injection
            // Kendi entity'si -> repository. Başka entity (Order) -> servis.
            private readonly IOrderItemRepository orderItemRepository;
            private readonly IOrderService orderService;
            public UpdateOrderItemCommandHandler(IOrderItemRepository orderItemRepository, IOrderService orderService)
            {
                this.orderItemRepository = orderItemRepository;
                this.orderService = orderService;
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

                decimal unitPrice = orderItem.TotalPrice.Amount / orderItem.Amount;
                decimal oldTotal = orderItem.TotalPrice.Amount;

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
