using ECommerce.Application.Exceptions;
using System.Diagnostics.CodeAnalysis;

namespace ECommerce.Application.Features.OrderItem.Rules
{
    // Sipariş kalemi (OrderItem) ile ilgili tüm iş kuralları burada yazılır.
    // Kalemler Order aggregate'inin parçasıdır: CreateOrderCommand de kalem oluşturduğu için bu kuralları kullanır.
    // Otomatik DI kaydı: BaseBusinessRules'tan türediği için AddSubClassesOfType ile Scoped eklenir.
    public class OrderItemBusinessRules : BaseBusinessRules
    {
        // [NotNull]: Metot hata fırlatmadan dönerse derleyici orderItem'ın artık null olmadığını bilir.
        public void OrderItemShouldExistWhenSelected([NotNull] Domain.Entities.OrderItem? orderItem)
        {
            if (orderItem is null)
                throw new NotFoundException("Order item not found.");
        }

        // Kalem adedi pozitif olmalı.
        public void AmountShouldBeGreaterThanZero(int amount)
        {
            if (amount <= 0)
                throw new BusinessException("Amount must be greater than zero.");
        }

        // Kalemdeki ürün, siparişin para biriminde olmalı.
        public void ProductCurrencyShouldMatchOrderCurrency(Domain.Entities.Product product, string orderCurrency)
        {
            if (product.Price.Currency != orderCurrency)
                throw new BusinessException("Product currency does not match the order currency.");
        }
    }
}
