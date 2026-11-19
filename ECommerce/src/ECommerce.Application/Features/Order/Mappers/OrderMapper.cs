using ECommerce.Application.Features.Order.Commands.Create;
using ECommerce.Application.Features.Order.Commands.Update;
using ECommerce.Application.Features.Order.Queries;
using ECommerce.Application.Features.OrderItem.Mappers;

namespace ECommerce.Application.Features.Order.Mappers
{
    // Order feature'ının tüm mapping'leri burada yazılır.
    // Command/Query handler'ları mapping yapmaz; bu metotları çağırır.
    public static class OrderMapper
    {
        // Siparişi kalemleriyle birlikte oluşturur (aggregate).
        // products: kalemlerdeki ürünler, ProductId'ye göre (handler kuralları geçirip servisten getirmiş olmalı).
        // userId: siparişin sahibi (isteği yapan kullanıcının token oturumundan).
        // OrderDate, fiyatlar ve sahip istemciden alınmaz, sunucu belirler.
        public static Domain.Entities.Order MapCreateCommandToOrder(
            CreateOrderCommand command,
            IReadOnlyDictionary<Guid, Domain.Entities.Product> products,
            Guid userId)
        {
            Domain.Entities.Order order = new()
            {
                OrderDate = DateTime.UtcNow,
                UserId = userId
            };

            foreach (CreateOrderCommand.CreateOrderItemDto item in command.Items)
            {
                // OrderId, EF tarafından navigation üzerinden atanır.
                order.Items.Add(OrderItemMapper.MapProductToOrderItem(products[item.ProductId], item.Amount));
            }

            order.TotalPrice = new Domain.ValueObjects.Money(order.Items.Sum(i => i.TotalPrice.Amount), command.Currency);

            return order;
        }

        // Var olan entity'nin üzerine yazar. TotalPrice burada güncellenmez; kalemlerden hesaplanır.
        public static void MapUpdateCommandToOrder(UpdateOrderCommand command, Domain.Entities.Order order)
        {
            order.OrderDate = command.OrderDate;
        }

        public static OrderResponse MapOrderToResponse(Domain.Entities.Order order)
        {
            return new OrderResponse
            {
                Id = order.Id,
                UserId = order.UserId,
                OrderDate = order.OrderDate,
                TotalPrice = order.TotalPrice.Amount,
                Currency = order.TotalPrice.Currency
            };
        }

        public static List<OrderResponse> MapOrdersToResponses(List<Domain.Entities.Order> orders)
        {
            return orders.Select(MapOrderToResponse).ToList();
        }
    }
}
