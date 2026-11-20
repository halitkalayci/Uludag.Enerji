using ECommerce.Application.Services.Repositories;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories
{
    // uludag-gen ile üretildi (spec 0005); serbestçe düzenlenebilir.
    // ICategoryRepository'nin EF Core ile yazılmış hali. Context'e yalnızca bu katman dokunur.
    public class CategoryRepository : ICategoryRepository
    {
        private readonly UludagDbContext uludagDbContext;
        public CategoryRepository(UludagDbContext uludagDbContext)
        {
            this.uludagDbContext = uludagDbContext;
        }

        public async Task<List<Category>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await uludagDbContext.Categories
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await uludagDbContext.Categories
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }

        public async Task AddAsync(Category category, CancellationToken cancellationToken = default)
        {
            await uludagDbContext.Categories.AddAsync(category, cancellationToken);
            await uludagDbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(Category category, CancellationToken cancellationToken = default)
        {
            uludagDbContext.Categories.Update(category);
            await uludagDbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Category category, CancellationToken cancellationToken = default)
        {
            uludagDbContext.Categories.Remove(category);
            await uludagDbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
