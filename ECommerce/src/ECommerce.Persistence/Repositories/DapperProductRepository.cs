using ECommerce.Application.Services.Repositories;
using ECommerce.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Persistence.Repositories
{
    public class DapperProductRepository : IProductRepository
    {
        public Task AddAsync(Product product, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(Product product, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<List<Product>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Product?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> AnyInCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
