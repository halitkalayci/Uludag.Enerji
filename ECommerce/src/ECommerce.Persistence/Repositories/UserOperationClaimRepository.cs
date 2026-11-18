using ECommerce.Application.Services.Repositories;
using ECommerce.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories
{
    // IUserOperationClaimRepository'nin EF Core ile yazılmış hali.
    // Context'e yalnızca bu katman dokunur.
    public class UserOperationClaimRepository : IUserOperationClaimRepository
    {
        private readonly UludagDbContext uludagDbContext;
        public UserOperationClaimRepository(UludagDbContext uludagDbContext)
        {
            this.uludagDbContext = uludagDbContext;
        }

        public async Task<List<string>> GetOperationClaimNamesByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await uludagDbContext.UserOperationClaims
                .AsNoTracking()
                .Where(uoc => uoc.UserId == userId)
                .Select(uoc => uoc.OperationClaim.Name)
                .ToListAsync(cancellationToken);
        }
    }
}
