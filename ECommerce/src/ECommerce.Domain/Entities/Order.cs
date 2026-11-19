using ECommerce.Domain.ValueObjects;

namespace ECommerce.Domain.Entities
{
    public class Order
    {
        public Guid Id { get; set; }
        public DateTime OrderDate { get; set; }
        public Money TotalPrice { get; set; } = default!;

        // Siparişin sahibi. İstemciden alınmaz; isteği yapan kullanıcının oturumundan (token) belirlenir.
        public Guid UserId { get; set; }

        // Navigation Properties
        public virtual User User { get; set; } = null!;

        // Sipariş, kalemleriyle birlikte tek seferde kaydedilebilsin diye (aggregate).
        public virtual ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}
