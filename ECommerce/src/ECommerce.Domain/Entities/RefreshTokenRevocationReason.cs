namespace ECommerce.Domain.Entities
{
    // Refresh token neden revoke edildi? Veritabanına ad olarak (string) yazılır.
    public enum RefreshTokenRevocationReason
    {
        // Kullanıldı ve yerine yeni bir token üretildi.
        Rotated,

        // Kullanıcı açıkça iptal etti (çıkış / revoke isteği).
        RevokedByUser,

        // Revoke edilmiş bir token tekrar kullanıldı -> çalınmış sayıldı, ailesiyle birlikte iptal edildi.
        ReuseDetected
    }
}
