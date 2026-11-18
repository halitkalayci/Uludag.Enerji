using ECommerce.Application.Features.Product.Rules;
using ECommerce.Application.Services.Repositories;

namespace ECommerce.Application.Services.ProductService
{
    // IProductService'in implementasyonu. Yalnızca kendi repository'sini (IProductRepository)
    // ve kendi feature'ının kurallarını (ProductBusinessRules) kullanır.
    public class ProductManager : IProductService
    {
        // Dependency Injection
        private readonly IProductRepository productRepository;
        private readonly ProductBusinessRules productBusinessRules;
        public ProductManager(IProductRepository productRepository, ProductBusinessRules productBusinessRules)
        {
            this.productRepository = productRepository;
            this.productBusinessRules = productBusinessRules;
        }

        public async Task<Domain.Entities.Product> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            Domain.Entities.Product? product = await productRepository.GetByIdAsync(id, cancellationToken);
            productBusinessRules.ProductShouldExistWhenSelected(product);
            return product;
        }

        public void StockShouldBeEnough(Domain.Entities.Product product, int amount)
        {
            productBusinessRules.StockShouldBeEnough(product, amount);
        }

        public async Task DecreaseStockAsync(Domain.Entities.Product product, int amount, CancellationToken cancellationToken = default)
        {
            productBusinessRules.StockChangeAmountShouldBeGreaterThanZero(amount);
            productBusinessRules.StockShouldBeEnough(product, amount);

            product.Stock -= amount;
            await productRepository.UpdateAsync(product, cancellationToken);
        }

        public async Task IncreaseStockAsync(Domain.Entities.Product product, int amount, CancellationToken cancellationToken = default)
        {
            productBusinessRules.StockChangeAmountShouldBeGreaterThanZero(amount);

            product.Stock += amount;
            await productRepository.UpdateAsync(product, cancellationToken);
        }

        public async Task ChangeStockByAmountDifferenceAsync(Domain.Entities.Product product, int amountDifference, CancellationToken cancellationToken = default)
        {
            // Adet azaldıysa (fark <= 0) stok kuralı her zaman sağlanır; arttıysa fark kadar stok gerekir.
            productBusinessRules.StockShouldBeEnough(product, amountDifference);

            product.Stock -= amountDifference;
            await productRepository.UpdateAsync(product, cancellationToken);
        }

        public async Task IncreaseStockIfProductExistsAsync(Guid productId, int amount, CancellationToken cancellationToken = default)
        {
            Domain.Entities.Product? product = await productRepository.GetByIdAsync(productId, cancellationToken);
            if (product is null)
                return;

            await IncreaseStockAsync(product, amount, cancellationToken);
        }
    }
}
