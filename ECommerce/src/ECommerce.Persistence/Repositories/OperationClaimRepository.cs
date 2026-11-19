using ECommerce.Application.Services.Repositories;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories
{
    // IOperationClaimRepository'nin EF Core ile yazılmış hali.
    // Context'e yalnızca bu katman dokunur.
    public class OperationClaimRepository : IOperationClaimRepository
    {
        private readonly UludagDbContext uludagDbContext;
        public OperationClaimRepository(UludagDbContext uludagDbContext)
        {
            this.uludagDbContext = uludagDbContext;
        }

        public async Task<OperationClaim?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            return await uludagDbContext.OperationClaims
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Name == name, cancellationToken);
        }
    }
}
