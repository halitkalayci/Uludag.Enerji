namespace ECommerce.Application.Features.Auth.Commands.RefreshToken
{
    // Refresh token kullanılınca dönen YENİ çift: JWT + refresh token.
    // Gönderilen refresh token artık geçersizdir; istemci bundan sonra bu cevaptaki RefreshToken'ı saklamalıdır.
    public class RefreshTokenResponse
    {
        public string AccessToken { get; set; } = null!;
        public DateTime Expiration { get; set; } // UTC

        public string RefreshToken { get; set; } = null!;
        public DateTime RefreshTokenExpiration { get; set; } // UTC
    }
}
