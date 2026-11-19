namespace ECommerce.Application.Features.Auth.Commands.VerifyOtpAuthenticator
{
    // MFA adımı tamamlanınca dönen model: girişin sonucu olan JWT.
    public class VerifyOtpAuthenticatorResponse
    {
        public string AccessToken { get; set; } = null!;
        public DateTime Expiration { get; set; } // UTC
    }
}
