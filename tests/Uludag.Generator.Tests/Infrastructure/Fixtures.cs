namespace Uludag.Generator.Tests.Infrastructure
{
    // Testlerde Domain'e eklenen fixture entity'ler (spec G-1: repoda kalıcı değiller).
    internal static class Fixtures
    {
        // AC-2 fixture'ı: skaler + tek Money + FK Guid + UserId + navigation + koleksiyon + enum + tanınmayan tip.
        public const string Category = """
            using ECommerce.Domain.ValueObjects;

            namespace ECommerce.Domain.Entities
            {
                public class Category
                {
                    public Guid Id { get; set; }
                    public string Name { get; set; } = null!;
                    public string? Description { get; set; }
                    public int DisplayOrder { get; set; }
                    public bool IsActive { get; set; }
                    public DateTime? PublishedAt { get; set; }
                    public CategoryStatus Status { get; set; }
                    public Money MinimumPrice { get; set; } = null!;
                    public Guid UserId { get; set; }
                    public Guid? ParentCategoryId { get; set; }
                    public byte[]? Image { get; set; }
                    public string DisplayName => Name;
                    public static int Counter { get; set; }

                    public virtual User User { get; set; } = null!;
                    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
                }

                public enum CategoryStatus
                {
                    Draft,
                    Published
                }
            }
            """;

        // Çalışırlık gözlemi (AC-3/AC-4) fixture'ı: AC-2 şekli (skaler + tek Money + FK Guid + navigation) ama
        // kimliğe bağlı FK yok — istemcinin verebileceği ProductId; üretilen kod elle düzenlenmeden uçtan uca
        // çalışır (review round 1, M-1). Seed edilmiş ürün: 2dcb27b9-06cf-4053-a423-c2e94ed048a6.
        public const string PriceAlert = """
            using ECommerce.Domain.ValueObjects;

            namespace ECommerce.Domain.Entities
            {
                public class PriceAlert
                {
                    public Guid Id { get; set; }
                    public string Label { get; set; } = null!;
                    public int Priority { get; set; }
                    public bool IsActive { get; set; }
                    public Money TargetPrice { get; set; } = null!;
                    public Guid ProductId { get; set; }

                    public virtual Product Product { get; set; } = null!;
                }
            }
            """;

        // İki Money: alanlar önekli düzleştirilir (spec R-4).
        public const string Coupon = """
            using ECommerce.Domain.ValueObjects;

            namespace ECommerce.Domain.Entities
            {
                public class Coupon
                {
                    public Guid Id { get; set; }
                    public string Code { get; set; } = null!;
                    public Money Discount { get; set; } = null!;
                    public Money MinimumOrder { get; set; } = null!;
                }
            }
            """;

        // Guid Id yok: reddedilir (spec G-6).
        public const string Tag = """
            namespace ECommerce.Domain.Entities
            {
                public class Tag
                {
                    public int Id { get; set; }
                    public string Name { get; set; } = null!;
                }
            }
            """;

        // Derlenmeyen kaynak: tanımsız tip ve hatalı metot gövdesi; okuma yine çalışmalı (AC-1).
        public const string Broken = """
            namespace ECommerce.Domain.Entities
            {
                public class Draft
                {
                    public Guid Id { get; set; }
                    public UndefinedType Thing { get; set; }
                    public int Calculate() => "not an int";
                }
            }
            """;
    }
}
