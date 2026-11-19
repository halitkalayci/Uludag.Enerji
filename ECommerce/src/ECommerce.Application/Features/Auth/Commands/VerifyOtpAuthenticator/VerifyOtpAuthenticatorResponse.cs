namespace ECommerce.Application.Features.Auth.Commands.VerifyOtpAuthenticator
{
    // MFA adımı tamamlanınca dönen model: girişin sonucu olan JWT + refresh token.
    // JWT'nin süresi dolunca RefreshToken ile /api/auth/refresh-token çağrılır ve yeni bir çift alınır.
    public class VerifyOtpAuthenticatorResponse
    {
        public string AccessToken { get; set; } = null!;
        public DateTime Expiration { get; set; } // UTC

        public string RefreshToken { get; set; } = null!;
        public DateTime RefreshTokenExpiration { get; set; } // UTC
    }
}
