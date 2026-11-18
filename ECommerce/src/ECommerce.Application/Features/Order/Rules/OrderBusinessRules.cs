using ECommerce.Application.Exceptions;
using System.Diagnostics.CodeAnalysis;

namespace ECommerce.Application.Features.Order.Rules
{
    // Sipariş (Order) ile ilgili tüm iş kuralları burada yazılır.
    // Handler'lar if-else ile durum kontrolü yapmaz; bu metotları çağırır.
    // Kural ihlal edilirse metot istisna fırlatır, edilmezse sessizce döner.
    // Otomatik DI kaydı: BaseBusinessRules'tan türediği için AddSubClassesOfType ile Scoped eklenir.
    public class OrderBusinessRules : BaseBusinessRules
    {
        // [NotNull]: Metot hata fırlatmadan dönerse derleyici order'ın artık null olmadığını bilir.
        public void OrderShouldExistWhenSelected([NotNull] Domain.Entities.Order? order)
        {
            if (order is null)
                throw new NotFoundException("Order not found.");
        }

        // Sipariş en az bir kalemle oluşturulmalı.
        public void OrderShouldHaveAtLeastOneItem<TItem>([NotNull] ICollection<TItem>? items)
        {
            if (items is null || items.Count == 0)
                throw new BusinessException("Order must contain at least one item.");
        }
    }
}
