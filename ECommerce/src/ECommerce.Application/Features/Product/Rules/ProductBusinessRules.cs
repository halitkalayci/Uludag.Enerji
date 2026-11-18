using ECommerce.Application.Exceptions;
using ECommerce.Application.Services.Repositories;
using System.Diagnostics.CodeAnalysis;

namespace ECommerce.Application.Features.Product.Rules
{
    // Bu projedeki tüm İş Kuralı classlarını tara. -> BaseBusinessRules türeyen tüm class ->
    public class ProductBusinessRules : BaseBusinessRules
    {
        private readonly IProductRepository productRepository;
        public ProductBusinessRules(IProductRepository productRepository) {
            this.productRepository = productRepository;
        }
        // ürün nesnesi ile ilgili tüm iş kuralları burada yazılır.
        // Diğer feature'lar (Order, OrderItem) bu kurallara doğrudan değil, IProductService üzerinden ulaşır.

        public async Task ProductNameCannotExist(string name)
        {
            // İş kuralı -> Sistemin durumuna göre bazen kural geçerli, bazen geçersiz.
            // Validasyon -> Sistem durumu gerekmez sadece inputtan geçerli veya geçersiz.

            // Ürün ismi 3 harften kısa olamaz -> Validasyon
            // Ürün ismi zaten var mı? -> Sisteme bakmam gerekiyor? DB'de daha önce "ürün 1" ismi var mı
            Domain.Entities.Product? productWithSameName = await productRepository.GetByNameAsync(name);
            if(productWithSameName != null)
                throw new BusinessException("Product name already exists.");
        }

        // [NotNull]: Metot hata fırlatmadan dönerse derleyici product'ın artık null olmadığını bilir.
        // Böylece handler'da kuraldan sonra "product!" yazmaya gerek kalmaz.
        public void ProductShouldExistWhenSelected([NotNull] Domain.Entities.Product? product)
        {
            if (product is null)
                throw new NotFoundException("Product not found.");
        }

        // Stoğa eklenen / stoktan düşülen adet pozitif olmalı.
        public void StockChangeAmountShouldBeGreaterThanZero(int amount)
        {
            if (amount <= 0)
                throw new BusinessException("Amount must be greater than zero.");
        }

        // Ürünün stoğu istenen adedi karşılamalı. (İstenen adet 0 ya da negatifse kural her zaman sağlanır.)
        public void StockShouldBeEnough(Domain.Entities.Product product, int requestedAmount)
        {
            if (product.Stock < requestedAmount)
                throw new BusinessException($"Insufficient stock for product {product.Id}.");
        }
    }
}
