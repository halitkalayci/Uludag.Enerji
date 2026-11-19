namespace ECommerce.Application.Features.Auth.Commands.Login
{
    // Şifre doğrulandıktan sonra dışarıya dönen model. JWT İÇERMEZ: giriş, MFA adımı tamamlanınca biter.
    //   MultiFactorStatus = SetupRequired -> MFA kurulmamış (ör. ilk giriş): önce /api/auth/mfa/setup, sonra /api/auth/mfa/verify.
    //   MultiFactorStatus = CodeRequired  -> MFA kurulu: authenticator kodu ile /api/auth/mfa/verify.
    // MultiFactorToken bu iki isteğe gönderilir; JWT /api/auth/mfa/verify'dan döner.
    public class LoginResponse
    {
        public MultiFactorStatus MultiFactorStatus { get; set; }
        public string MultiFactorToken { get; set; } = null!;
        public DateTime MultiFactorTokenExpiration { get; set; } // UTC
    }
}
