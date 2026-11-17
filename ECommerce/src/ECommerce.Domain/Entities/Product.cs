using ECommerce.Domain.ValueObjects;

namespace ECommerce.Domain.Entities
{
    // POJO -> Plain Old Java Object -> Bilgisayarımda yalnızca Java (C#) olsa bile çalışacak kod.
    public class Product
    {
        public Guid Id { get; set; }
        public Money Price { get; set; }
        public string? Name { get; set; }
        public int Stock { get; set; }
    }
}
