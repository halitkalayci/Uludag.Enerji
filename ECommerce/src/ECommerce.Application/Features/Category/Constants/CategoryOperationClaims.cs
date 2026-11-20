namespace ECommerce.Application.Features.Category.Constants
{
    // uludag-gen ile üretildi (spec 0005); serbestçe düzenlenebilir.
    // Category işlemlerinin operation claim adları. Karşılaştırma metinseldir (birebir, büyük/küçük harf duyarlı).
    // Varsayılan yapı:
    //   Create / Update / Delete -> Category.Admin, Category.Write, Category.<İşlem>
    //   GetAll / GetById         -> Category.Admin, Category.Read
    // Claim'ler OperationClaimSeeds ile veritabanına yazılır.
    public static class CategoryOperationClaims
    {
        public const string Admin = "Category.Admin";
        public const string Write = "Category.Write";
        public const string Read = "Category.Read";
        public const string Create = "Category.Create";
        public const string Update = "Category.Update";
        public const string Delete = "Category.Delete";
    }
}
