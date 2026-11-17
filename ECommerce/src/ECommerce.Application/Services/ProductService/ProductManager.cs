using ECommerce.Application.Services.Repositories;

namespace ECommerce.Application.Services.ProductService
{
    // IProductService'in implementasyonu. Yalnızca kendi repository'sini (IProductRepository) kullanır.
    public class ProductManager : IProductService
    {
        // Dependency Injection
        private readonly IProductRepository productRepository;
        public ProductManager(IProductRepository productRepository)
        {
            this.productRepository = productRepository;
        }

        public async Task<Domain.Entities.Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await productRepository.GetByIdAsync(id, cancellationToken);
        }

        public bool HasEnoughStock(Domain.Entities.Product product, int amount)
        {
            return product.Stock >= amount;
        }

        public async Task DecreaseStockAsync(Domain.Entities.Product product, int amount, CancellationToken cancellationToken = default)
        {
            if (amount <= 0)
                throw new ArgumentException("Amount must be greater than zero.");

            if (!HasEnoughStock(product, amount))
                throw new ArgumentException($"Insufficient stock for product {product.Id}.");

            product.Stock -= amount;
            await productRepository.UpdateAsync(product, cancellationToken);
        }

        public async Task IncreaseStockAsync(Domain.Entities.Product product, int amount, CancellationToken cancellationToken = default)
        {
            if (amount <= 0)
                throw new ArgumentException("Amount must be greater than zero.");

            product.Stock += amount;
            await productRepository.UpdateAsync(product, cancellationToken);
        }
    }
}
