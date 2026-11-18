using ECommerce.Domain.Entities;
using ECommerce.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Uludag.Core.Security.Hashing;

namespace ECommerce.Persistence.Seeds
{
    // Uygulama açılışında ilk yöneticiyi hazırlar.
    // Neden gerekli? Register olan kullanıcının hiç claim'i yoktur; kimseye claim atanmamışsa
    // korumalı her istek herkese 403 döner. Bu seeder, ayarlardaki kullanıcıya global "Admin" claim'ini verir.
    // Neden migration (HasData) değil? Şifre hash'i migration'a sabit yazılırsa repoya girer; burada ayardan okunur.
    // Tekrar çalıştırmak güvenlidir: kullanıcı ve atama zaten varsa yeniden eklenmez.
    public static class AdminUserSeeder
    {
        public static async Task SeedAdminUserAsync(this IServiceProvider serviceProvider, AdminSeedOptions? options)
        {
            // Ayar yok (ör. production'da tanımlanmamış) -> hiçbir şey yapma.
            if (options is null || string.IsNullOrWhiteSpace(options.Email) || string.IsNullOrWhiteSpace(options.Password))
                return;

            // DbContext Scoped -> uygulama açılışında istek yok, scope'u kendimiz açarız.
            using IServiceScope scope = serviceProvider.CreateScope();
            UludagDbContext context = scope.ServiceProvider.GetRequiredService<UludagDbContext>();

            User? admin = await context.Users
                .Include(u => u.UserOperationClaims)
                .FirstOrDefaultAsync(u => u.Email == options.Email);

            if (admin is null)
            {
                HashingHelper.CreatePasswordHash(options.Password, out byte[] passwordHash, out byte[] passwordSalt);
                admin = new User
                {
                    Id = Guid.NewGuid(),
                    Email = options.Email,
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt
                };
                await context.Users.AddAsync(admin);
            }

            if (!admin.UserOperationClaims.Any(uoc => uoc.OperationClaimId == OperationClaimSeeds.AdminId))
            {
                admin.UserOperationClaims.Add(new UserOperationClaim
                {
                    Id = Guid.NewGuid(),
                    UserId = admin.Id,
                    OperationClaimId = OperationClaimSeeds.AdminId
                });
            }

            await context.SaveChangesAsync();
        }
    }
}
