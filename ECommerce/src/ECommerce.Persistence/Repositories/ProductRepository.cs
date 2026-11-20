using ECommerce.Application.Services.Repositories;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories
{
    // IProductRepository'nin EF Core ile yazılmış hali.
    // Context'e yalnızca bu katman dokunur.
    public class ProductRepository : IProductRepository
    {
        private readonly UludagDbContext uludagDbContext;
        public ProductRepository(UludagDbContext uludagDbContext)
        {
            this.uludagDbContext = uludagDbContext;
        }

        public async Task<List<Product>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            // Yanıt ürünün kategori adını da taşır (spec 0009, R-5).
            return await uludagDbContext.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .ToListAsync(cancellationToken);
        }

        public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await uludagDbContext.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
        {
            await uludagDbContext.Products.AddAsync(product, cancellationToken);
            await uludagDbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
        {
            uludagDbContext.Products.Update(product);
            await uludagDbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Product product, CancellationToken cancellationToken = default)
        {
            uludagDbContext.Products.Remove(product);
            await uludagDbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> AnyInCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
        {
            return await uludagDbContext.Products.AnyAsync(p => p.CategoryId == categoryId, cancellationToken);
        }

        public async Task<Product?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            return await uludagDbContext.Products.FirstOrDefaultAsync(p => p.Name == name);
        }
    }
}
