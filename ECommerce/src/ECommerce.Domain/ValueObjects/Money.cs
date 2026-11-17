using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.ValueObjects
{
    // VO => Class değildir
    // "sealed"
    // record => 
    public sealed record Money
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "TRY";

        // Para birimi ve miktarını temsil eden bir değer nesnesi.
        // Kurallar yalnızca bu sınıfın içinde tanımlanır.

        // Saf C# dışına çıkma. Kütüphane kullanma.
        public Money(decimal amount, string currency) {
            if(amount < 0)
                throw new ArgumentException("Amount cannot be negative.");
            Amount = amount;
            Currency = currency;
        }
    }
}
