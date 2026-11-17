using ECommerce.Domain.ValueObjects;

namespace ECommerce.Domain.Entities
{
    public class Order
    {
        public Guid Id { get; set; }
        public DateTime OrderDate { get; set; }
        public Money TotalPrice { get; set; } = default!;
        // ......
    }
}
