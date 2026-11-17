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
    }
}
