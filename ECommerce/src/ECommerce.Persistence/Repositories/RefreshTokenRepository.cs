using ECommerce.Application.Services.Repositories;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories
{
    // IRefreshTokenRepository'nin EF Core ile yazılmış hali.
    // Context'e yalnızca bu katman dokunur.
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly UludagDbContext uludagDbContext;
        public RefreshTokenRepository(UludagDbContext uludagDbContext)
        {
            this.uludagDbContext = uludagDbContext;
        }

        public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        {
            return await uludagDbContext.RefreshTokens
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.TokenHash == tokenHash, cancellationToken);
        }

        public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
        {
            await uludagDbContext.RefreshTokens.AddAsync(refreshToken, cancellationToken);
            await uludagDbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> TryRevokeActiveAsync(
            Guid id,
            RefreshTokenRevocationReason reason,
            Guid? replacedByTokenId = null,
            CancellationToken cancellationToken = default)
        {
            DateTime now = DateTime.UtcNow;

            // Tek SQL: UPDATE ... WHERE Id = @id AND RevokedAt IS NULL AND ExpiresAt > @now.
            // Koşul UPDATE'in içinde olduğu için aynı token ile eşzamanlı iki istekten yalnızca biri 1 satır günceller.
            int affectedRows = await uludagDbContext.RefreshTokens
                .Where(r => r.Id == id && r.RevokedAt == null && r.ExpiresAt > now)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(r => r.RevokedAt, now)
                    .SetProperty(r => r.RevocationReason, reason)
                    .SetProperty(r => r.ReplacedByTokenId, replacedByTokenId), cancellationToken);

            return affectedRows > 0;
        }

        public async Task RevokeActiveFamilyAsync(
            Guid familyId,
            RefreshTokenRevocationReason reason,
            CancellationToken cancellationToken = default)
        {
            DateTime now = DateTime.UtcNow;

            // Tek SQL: UPDATE ... WHERE FamilyId = @familyId AND RevokedAt IS NULL.
            await uludagDbContext.RefreshTokens
                .Where(r => r.FamilyId == familyId && r.RevokedAt == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(r => r.RevokedAt, now)
                    .SetProperty(r => r.RevocationReason, reason), cancellationToken);
        }

        public async Task RevokeAllActiveByUserIdAsync(
            Guid userId,
            RefreshTokenRevocationReason reason,
            CancellationToken cancellationToken = default)
        {
            DateTime now = DateTime.UtcNow;

            // Tek SQL: UPDATE ... WHERE UserId = @userId AND RevokedAt IS NULL (UserId foreign key index'ini kullanır).
            // Süresi dolmuş ama revoke edilmemiş token'lar da işaretlenir -> denetimde "neden kapandı" sorusu cevapsız kalmaz.
            await uludagDbContext.RefreshTokens
                .Where(r => r.UserId == userId && r.RevokedAt == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(r => r.RevokedAt, now)
                    .SetProperty(r => r.RevocationReason, reason), cancellationToken);
        }
    }
}
