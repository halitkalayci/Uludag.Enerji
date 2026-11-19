using Uludag.Core.Security.JWT;

namespace ECommerce.Application.Features.Auth.Mappers
{
    // Auth feature'ının RefreshToken mapping'leri burada yazılır.
    // Command/Query handler'ları mapping yapmaz; bu metotları çağırır.
    public static class RefreshTokenMapper
    {
        // Girişte (MFA doğrulaması) üretilen ilk token -> yeni bir aile başlatır.
        public static Domain.Entities.RefreshToken MapToNewFamilyRefreshToken(Guid userId, IssuedRefreshToken issuedRefreshToken)
        {
            return MapToRefreshToken(userId, Guid.NewGuid(), issuedRefreshToken);
        }

        // Rotasyonda üretilen token -> eski token'ın ailesinde kalır.
        // Düz token entity'ye geçmez; yalnızca özeti (TokenHash) saklanır.
        public static Domain.Entities.RefreshToken MapToRefreshToken(Guid userId, Guid familyId, IssuedRefreshToken issuedRefreshToken)
        {
            return new Domain.Entities.RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                FamilyId = familyId,
                TokenHash = issuedRefreshToken.TokenHash,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = issuedRefreshToken.Expiration
            };
        }
    }
}
