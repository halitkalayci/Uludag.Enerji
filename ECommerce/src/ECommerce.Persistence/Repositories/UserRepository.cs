using ECommerce.Application.Services.Repositories;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Contexts;
using ECommerce.Application.Exceptions;
using ECommerce.Application.Features.Auth.Constants;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories
{
    // IUserRepository'nin EF Core ile yazılmış hali.
    // Context'e yalnızca bu katman dokunur.
    public class UserRepository : IUserRepository
    {
        private readonly UludagDbContext uludagDbContext;
        public UserRepository(UludagDbContext uludagDbContext)
        {
            this.uludagDbContext = uludagDbContext;
        }

        public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return await uludagDbContext.Users
                .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        }

        public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await uludagDbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        }

        public async Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            await uludagDbContext.Users.AddAsync(user, cancellationToken);
            try
            {
                await uludagDbContext.SaveChangesAsync(cancellationToken);
            }
            // BR-8: aynı e-postayla eşzamanlı iki kayıt, UserEmailCannotExist kontrolünü birlikte geçebilir; ikincisini
            // veritabanındaki unique index durdurur. Kısıt ihlali, kuralın kendi hatasına (400) çevrilir — 500 değil.
            catch (DbUpdateException exception) when (IsUniqueViolation(exception))
            {
                throw new BusinessException(AuthConstants.UserEmailAlreadyExists);
            }
        }

        // SQL Server: 2601 = unique index ihlali, 2627 = unique constraint ihlali. Users tablosundaki tek unique index
        // Email'dir (UserConfiguration); Id her kayıtta yeni Guid'dir.
        private static bool IsUniqueViolation(DbUpdateException exception) =>
            exception.InnerException is SqlException { Number: 2601 or 2627 };
    }
}
