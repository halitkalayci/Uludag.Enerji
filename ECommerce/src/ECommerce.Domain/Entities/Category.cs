namespace ECommerce.Domain.Entities
{
    // Ürünlerin gruplandığı kategori (spec 0009). Tek iş alanı addır; ad benzersizdir (BR-12).
    // Bir ürünün en fazla bir kategorisi vardır (BR-13); ürünü olan kategori silinemez (BR-14).
    public class Category
    {
        // Adın en fazla uzunluğu — kural (CategoryBusinessRules) ve kolon (CategoryConfiguration) buradan okur.
        public const int NameMaxLength = 100;

        public Guid Id { get; set; }
        public string Name { get; set; } = null!;

        // Navigation Properties
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
