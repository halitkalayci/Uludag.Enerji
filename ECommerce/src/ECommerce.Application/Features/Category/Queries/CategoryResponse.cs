namespace ECommerce.Application.Features.Category.Queries
{
    // uludag-gen ile üretildi (spec 0005); serbestçe düzenlenebilir.
    // Sorguların dışarıya döndüğü model. Entity doğrudan dışarı açılmaz; Money düz alanlara açılır.
    public class CategoryResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
    }
}
