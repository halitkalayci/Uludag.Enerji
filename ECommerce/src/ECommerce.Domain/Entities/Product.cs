using ECommerce.Domain.ValueObjects;

namespace ECommerce.Domain.Entities
{
    // POJO -> Plain Old Java Object -> Bilgisayarımda yalnızca Java (C#) olsa bile çalışacak kod.
    public class Product
    {
        public Guid Id { get; set; }
        public Money Price { get; set; } = null!;
        public string? Name { get; set; }
        public int Stock { get; set; }

        // Ürünün en fazla bir kategorisi vardır (BR-13); null = kategorisiz.
        public Guid? CategoryId { get; set; }

        // Navigation Properties
        public virtual Category? Category { get; set; }
    }
}
