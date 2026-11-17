namespace ECommerce.Application.Features.OrderItem.Queries
{
    // Sorguların dışarıya döndüğü model.
    // Entity'yi doğrudan dışarı açmıyoruz; Money VO'yu düz alanlara açıyoruz.
    public class OrderItemResponse
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public Guid ProductId { get; set; }
        public int Amount { get; set; } // adet
        public decimal TotalPrice { get; set; }
        public string Currency { get; set; } = null!;
    }
}
