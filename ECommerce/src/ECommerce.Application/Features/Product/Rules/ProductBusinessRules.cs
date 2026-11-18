using ECommerce.Application.Services.Repositories;
using ECommerce.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public async Task ProductNameCannotExist(string name)
        {
            // İş kuralı -> Sistemin durumuna göre bazen kural geçerli, bazen geçersiz.
            // Validasyon -> Sistem durumu gerekmez sadece inputtan geçerli veya geçersiz.

            // Ürün ismi 3 harften kısa olamaz -> Validasyon
            // Ürün ismi zaten var mı? -> Sisteme bakmam gerekiyor? DB'de daha önce "ürün 1" ismi var mı
            Domain.Entities.Product? productWithSameName = await productRepository.GetByNameAsync(name);
            if(productWithSameName != null)
                throw new Exception("Product name already exists.");
        }
    }
}
