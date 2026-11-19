namespace ECommerce.Application.Features.Order.Queries
{
    // Sorguların dışarıya döndüğü model.
    // Entity'yi doğrudan dışarı açmıyoruz; Money VO'yu düz alanlara açıyoruz.
    public class OrderResponse
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalPrice { get; set; }
        public string Currency { get; set; } = null!;
    }
}
