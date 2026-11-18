using ECommerce.Application.Features.OrderItem.Commands.Create;
using ECommerce.Application.Features.OrderItem.Commands.Update;
using ECommerce.Application.Features.OrderItem.Queries;

namespace ECommerce.Application.Features.OrderItem.Mappers
{
    // OrderItem feature'ının tüm mapping'leri burada yazılır.
    // Command/Query handler'ları mapping yapmaz; bu metotları çağırır.
    // Kalemler Order aggregate'inin parçası olduğu için OrderMapper da kalem oluştururken bu mapper'ı kullanır.
    public static class OrderItemMapper
    {
        // Fiyat istemciden alınmaz; ürünün güncel fiyatından hesaplanır.
        // Her entity kendi Money örneğini almalı; product.Price paylaşılmaz.
        // OrderId atanmaz: sipariş oluşturulurken EF navigation üzerinden atar, tek kalem eklenirken çağıran atar.
        public static Domain.Entities.OrderItem MapProductToOrderItem(Domain.Entities.Product product, int amount)
        {
            return new Domain.Entities.OrderItem
            {
                ProductId = product.Id,
                Amount = amount,
                TotalPrice = new Domain.ValueObjects.Money(product.Price.Amount * amount, product.Price.Currency)
            };
        }

        public static Domain.Entities.OrderItem MapCreateCommandToOrderItem(CreateOrderItemCommand command, Domain.Entities.Product product)
        {
            Domain.Entities.OrderItem orderItem = MapProductToOrderItem(product, command.Amount);
            orderItem.OrderId = command.OrderId;
            return orderItem;
        }

        // Var olan entity'nin üzerine yazar. Birim fiyat, kalem eklendiği andaki fiyattır
        // (ürün fiyatı sonradan değişse de kalem etkilenmez); yeni tutar bu birim fiyattan hesaplanır.
        public static void MapUpdateCommandToOrderItem(UpdateOrderItemCommand command, Domain.Entities.OrderItem orderItem)
        {
            decimal unitPrice = orderItem.TotalPrice.Amount / orderItem.Amount;

            orderItem.Amount = command.Amount;
            orderItem.TotalPrice = new Domain.ValueObjects.Money(unitPrice * command.Amount, orderItem.TotalPrice.Currency);
        }

        public static OrderItemResponse MapOrderItemToResponse(Domain.Entities.OrderItem orderItem)
        {
            return new OrderItemResponse
            {
                Id = orderItem.Id,
                OrderId = orderItem.OrderId,
                ProductId = orderItem.ProductId,
                Amount = orderItem.Amount,
                TotalPrice = orderItem.TotalPrice.Amount,
                Currency = orderItem.TotalPrice.Currency
            };
        }

        public static List<OrderItemResponse> MapOrderItemsToResponses(List<Domain.Entities.OrderItem> orderItems)
        {
            return orderItems.Select(MapOrderItemToResponse).ToList();
        }
    }
}
