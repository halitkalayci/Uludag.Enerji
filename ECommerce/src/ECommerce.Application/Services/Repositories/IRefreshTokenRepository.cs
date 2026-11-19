namespace ECommerce.Application.Services.Repositories
{
    // RefreshToken tablosu için önceden yazılmış komutların sözleşmesi.
    // Implementasyon Persistence katmanındadır (Dependency Inversion).
    public interface IRefreshTokenRepository
    {
        // Token özetine göre kayıt (aktif / revoke edilmiş / süresi dolmuş fark etmez).
        Task<Domain.Entities.RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

        Task AddAsync(Domain.Entities.RefreshToken refreshToken, CancellationToken cancellationToken = default);

        // Token'ı YALNIZCA hâlâ aktifse (revoke edilmemiş ve süresi dolmamış) revoke eder; tek atomik SQL.
        // true -> bu çağrı revoke etti. false -> token zaten revoke edilmiş / süresi dolmuş
        // (ör. aynı token ile eşzamanlı gelen başka bir istek önce davrandı).
        Task<bool> TryRevokeActiveAsync(
            Guid id,
            Domain.Entities.RefreshTokenRevocationReason reason,
            Guid? replacedByTokenId = null,
            CancellationToken cancellationToken = default);

        // Ailenin (aynı girişten türeyen) tüm aktif token'larını revoke eder.
        Task RevokeActiveFamilyAsync(
            Guid familyId,
            Domain.Entities.RefreshTokenRevocationReason reason,
            CancellationToken cancellationToken = default);

        // Kullanıcının (tüm ailelerdeki) bütün aktif token'larını revoke eder; tek atomik SQL (ör. logout).
        Task RevokeAllActiveByUserIdAsync(
            Guid userId,
            Domain.Entities.RefreshTokenRevocationReason reason,
            CancellationToken cancellationToken = default);
    }
}
