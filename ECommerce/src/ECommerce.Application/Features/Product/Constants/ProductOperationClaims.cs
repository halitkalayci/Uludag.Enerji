namespace ECommerce.Application.Features.Product.Constants
{
    // Ürün işlemlerinin operation claim adları. Karşılaştırma metinseldir (birebir, büyük/küçük harf duyarlı).
    // Varsayılan yapı:
    //   Create / Update / Delete -> Product.Admin, Product.Write, Product.<İşlem>
    //   GetAll / GetById         -> Product.Admin, Product.Read
    public static class ProductOperationClaims
    {
        public const string Admin = "Product.Admin";
        public const string Write = "Product.Write";
        public const string Read = "Product.Read";
        public const string Create = "Product.Create";
        public const string Update = "Product.Update";
        public const string Delete = "Product.Delete";
    }
}
