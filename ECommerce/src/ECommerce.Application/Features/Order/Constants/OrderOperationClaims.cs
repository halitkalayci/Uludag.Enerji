namespace ECommerce.Application.Features.Order.Constants
{
    // Sipariş işlemlerinin operation claim adları. Karşılaştırma metinseldir (birebir, büyük/küçük harf duyarlı).
    // Varsayılan yapı:
    //   Create / Update / Delete -> Order.Admin, Order.Write, Order.<İşlem>
    //   GetAll / GetById         -> Order.Admin, Order.Read
    public static class OrderOperationClaims
    {
        public const string Admin = "Order.Admin";
        public const string Write = "Order.Write";
        public const string Read = "Order.Read";
        public const string Create = "Order.Create";
        public const string Update = "Order.Update";
        public const string Delete = "Order.Delete";
    }
}
