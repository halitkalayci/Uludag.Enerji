namespace ECommerce.Domain.Entities
{
    // Kullanıcının refresh token kaydı. Ömür: TokenOptions.RefreshTokenExpiration (3 gün).
    // Token'ın kendisi saklanmaz, yalnızca SHA-256 özeti (TokenHash) saklanır.
    //
    // Rotasyon: her kullanımda bu kayıt revoke edilir (RevocationReason = Rotated, ReplacedByTokenId = yeni kayıt)
    // ve aynı aileden (FamilyId) yeni bir kayıt açılır; istemciye yeni bir JWT + refresh token çifti döner.
    // Aile: tek bir girişten (MFA doğrulaması) türeyen tüm token'lar aynı FamilyId'yi taşır.
    // Revoke edilmiş bir token tekrar kullanılırsa token çalınmış sayılır ve ailenin aktif token'ı da revoke edilir.
    public class RefreshToken
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid FamilyId { get; set; }

        public string TokenHash { get; set; } = null!;
        public DateTime CreatedAt { get; set; }  // UTC
        public DateTime ExpiresAt { get; set; }  // UTC

        // null -> aktif (süresi de dolmamışsa kullanılabilir).
        public DateTime? RevokedAt { get; set; } // UTC
        public RefreshTokenRevocationReason? RevocationReason { get; set; }

        // Rotasyonda bu token'ın yerine üretilen kayıt (denetim / iz sürme için).
        public Guid? ReplacedByTokenId { get; set; }

        // Navigation Properties
        public virtual User User { get; set; } = null!;
    }
}
