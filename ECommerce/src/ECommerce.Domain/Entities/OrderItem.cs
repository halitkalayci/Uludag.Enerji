using ECommerce.Domain.ValueObjects;

namespace ECommerce.Domain.Entities
{
    public class OrderItem
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public Guid ProductId { get; set; }
        public int Amount { get; set; }
        public Money TotalPrice { get; set; } = default!;

        // Navigation Properties
        public virtual Order Order { get; set; } = default!;
        public virtual Product Product { get; set; } = default!;
    }
}
