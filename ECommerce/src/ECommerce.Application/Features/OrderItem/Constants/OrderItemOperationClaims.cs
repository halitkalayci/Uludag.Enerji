namespace ECommerce.Application.Features.OrderItem.Constants
{
    // Sipariş kalemi işlemlerinin operation claim adları. Karşılaştırma metinseldir (birebir, büyük/küçük harf duyarlı).
    // Varsayılan yapı:
    //   Create / Update / Delete -> OrderItem.Admin, OrderItem.Write, OrderItem.<İşlem>
    //   GetAll / GetById         -> OrderItem.Admin, OrderItem.Read
    public static class OrderItemOperationClaims
    {
        public const string Admin = "OrderItem.Admin";
        public const string Write = "OrderItem.Write";
        public const string Read = "OrderItem.Read";
        public const string Create = "OrderItem.Create";
        public const string Update = "OrderItem.Update";
        public const string Delete = "OrderItem.Delete";
    }
}
