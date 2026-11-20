namespace ECommerce.Application.Features.Product.Queries
{
    // Sorguların dışarıya döndüğü model.
    // Entity'yi doğrudan dışarı açmıyoruz; Money VO'yu düz alanlara açıyoruz (CreateProductCommand ile aynı şekil).
    public class ProductResponse
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = null!;
        public int Stock { get; set; }
        // Kategorisiz üründe ikisi de null (spec 0009, R-5).
        public Guid? CategoryId { get; set; }
        public string? CategoryName { get; set; }
    }
}
