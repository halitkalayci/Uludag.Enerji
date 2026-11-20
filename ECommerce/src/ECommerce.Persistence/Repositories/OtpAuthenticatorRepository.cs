using ECommerce.Application.Services.Repositories;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories
{
    // IOtpAuthenticatorRepository'nin EF Core ile yazılmış hali.
    // Context'e yalnızca bu katman dokunur.
    public class OtpAuthenticatorRepository : IOtpAuthenticatorRepository
    {
        private readonly UludagDbContext uludagDbContext;
        public OtpAuthenticatorRepository(UludagDbContext uludagDbContext)
        {
            this.uludagDbContext = uludagDbContext;
        }

        public async Task<OtpAuthenticator?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await uludagDbContext.OtpAuthenticators
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.UserId == userId, cancellationToken);
        }

        public async Task<bool> IsVerifiedAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await uludagDbContext.OtpAuthenticators
                .AnyAsync(o => o.UserId == userId && o.IsVerified, cancellationToken);
        }

        public async Task AddAsync(OtpAuthenticator otpAuthenticator, CancellationToken cancellationToken = default)
        {
            await uludagDbContext.OtpAuthenticators.AddAsync(otpAuthenticator, cancellationToken);
            await uludagDbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteUnverifiedByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            // Tek SQL: DELETE ... WHERE UserId = @userId AND IsVerified = 0 (entity belleğe yüklenmez).
            await uludagDbContext.OtpAuthenticators
                .Where(o => o.UserId == userId && !o.IsVerified)
                .ExecuteDeleteAsync(cancellationToken);
        }

        public async Task MarkAsVerifiedAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            // Tek SQL: UPDATE ... SET IsVerified = 1 WHERE UserId = @userId.
            await uludagDbContext.OtpAuthenticators
                .Where(o => o.UserId == userId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(o => o.IsVerified, true), cancellationToken);
        }

        public async Task<bool> TryUseTimeStepAsync(Guid userId, long timeStep, CancellationToken cancellationToken = default)
        {
            // Tek SQL, koşullu: UPDATE ... SET LastUsedTimeStep = @step
            //   WHERE UserId = @userId AND (LastUsedTimeStep IS NULL OR LastUsedTimeStep < @step).
            // Karşılaştırma ve yazma veritabanında tek adımda olduğu için eşzamanlı tekrar kullanım da reddedilir.
            int affectedRows = await uludagDbContext.OtpAuthenticators
                .Where(o => o.UserId == userId && (o.LastUsedTimeStep == null || o.LastUsedTimeStep < timeStep))
                .ExecuteUpdateAsync(setters => setters.SetProperty(o => o.LastUsedTimeStep, timeStep), cancellationToken);
            return affectedRows > 0;
        }
    }
}
