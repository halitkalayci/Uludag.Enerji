using ECommerce.Application.Services.Repositories;
using ECommerce.Domain.Entities;
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

        public async Task<bool> AnyAsync(Guid userId, Guid operationClaimId, CancellationToken cancellationToken = default)
        {
            return await uludagDbContext.UserOperationClaims
                .AnyAsync(uoc => uoc.UserId == userId && uoc.OperationClaimId == operationClaimId, cancellationToken);
        }

        public async Task AddAsync(UserOperationClaim userOperationClaim, CancellationToken cancellationToken = default)
        {
            await uludagDbContext.UserOperationClaims.AddAsync(userOperationClaim, cancellationToken);
            await uludagDbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
