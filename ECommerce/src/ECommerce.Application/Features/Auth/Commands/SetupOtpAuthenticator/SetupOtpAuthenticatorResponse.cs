namespace ECommerce.Application.Features.Auth.Commands.SetupOtpAuthenticator
{
    // MFA kurulumunda kullanıcıya gösterilecek bilgiler. Yalnızca bir kez döner; sonradan tekrar okunamaz.
    public class SetupOtpAuthenticatorResponse
    {
        // Base32 gizli anahtar -> QR okutulamazsa authenticator uygulamasına elle girilir.
        public string SecretKey { get; set; } = null!;

        // "otpauth://totp/..." adresi -> istemci bunu QR koda çevirip gösterir.
        public string QrCodeUri { get; set; } = null!;
    }
}
