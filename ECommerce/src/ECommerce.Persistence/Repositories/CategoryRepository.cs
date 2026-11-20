using ECommerce.Application.Exceptions;
using ECommerce.Application.Features.Category.Constants;
using ECommerce.Application.Services.Repositories;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Contexts;
using Microsoft.Data.SqlClient;
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

        public async Task<Category?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            return await uludagDbContext.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Name == name, cancellationToken);
        }

        public async Task AddAsync(Category category, CancellationToken cancellationToken = default)
        {
            await uludagDbContext.Categories.AddAsync(category, cancellationToken);
            await SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(Category category, CancellationToken cancellationToken = default)
        {
            uludagDbContext.Categories.Update(category);
            await SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Category category, CancellationToken cancellationToken = default)
        {
            uludagDbContext.Categories.Remove(category);
            await SaveChangesAsync(cancellationToken);
        }

        // Kural kontrolü ile kayıt arasındaki yarışı veritabanı kısıtları durdurur; ihlal, aynı kuralın 400'üne
        // çevrilir — 500 değil (docs/conventions.md, Known traps):
        //   2601/2627 = unique index ihlali -> BR-12 (Categories'teki tek unique index Name'dir)
        //   547       = FK ihlali (silinen kategoriye o arada ürün bağlandı; Restrict) -> BR-14
        private async Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            try
            {
                await uludagDbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
            {
                throw new BusinessException(CategoryConstants.CategoryNameAlreadyExists);
            }
            catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 547 })
            {
                throw new BusinessException(CategoryConstants.CategoryHasProducts);
            }
        }
    }
}
