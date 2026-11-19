namespace ECommerce.Application.Services.Repositories
{
    // OtpAuthenticator tablosu için önceden yazılmış komutların sözleşmesi.
    // Implementasyon Persistence katmanındadır (Dependency Inversion).
    public interface IOtpAuthenticatorRepository
    {
        Task<Domain.Entities.OtpAuthenticator?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

        // Kullanıcının MFA'sı aktif mi? (doğrulanmış authenticator kaydı var mı)
        Task<bool> IsVerifiedAsync(Guid userId, CancellationToken cancellationToken = default);

        Task AddAsync(Domain.Entities.OtpAuthenticator otpAuthenticator, CancellationToken cancellationToken = default);

        // Yarım kalmış (doğrulanmamış) kurulumu siler. Doğrulanmış kayda asla dokunmaz.
        Task DeleteUnverifiedByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

        // Kurulumu tamamlar: IsVerified = true. Zaten doğrulanmışsa değişiklik olmaz.
        Task MarkAsVerifiedAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
