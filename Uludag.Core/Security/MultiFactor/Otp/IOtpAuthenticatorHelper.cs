namespace Uludag.Core.Security.MultiFactor.Otp
{
    // TOTP (Time-based One-Time Password, RFC 6238) sözleşmesi. Authenticator uygulamalarının ürettiği
    // 30 saniyelik 6 haneli kodlar bununla üretilir / doğrulanır.
    // Uygulama katmanı yalnızca bu arayüzü bilir; hangi kütüphanenin (OtpNet) kullanıldığını bilmez.
    public interface IOtpAuthenticatorHelper
    {
        // Kullanıcıya özel rastgele gizli anahtar (secret). Veritabanında saklanır;
        // hem sunucu hem authenticator uygulaması kodu bu anahtardan üretir.
        byte[] GenerateSecretKey();

        // Gizli anahtarın Base32 metni. QR okutulamadığında kullanıcı bunu uygulamaya elle girer.
        string ConvertSecretKeyToString(byte[] secretKey);

        // QR koda çevrilecek "otpauth://totp/..." adresi. accountName -> uygulamada görünen hesap adı (ör. e-posta).
        string CreateKeyUri(string accountName, byte[] secretKey);

        // Kullanıcının girdiği kod bu anahtarla, şu an (± VerificationWindow adım) için geçerli mi?
        bool VerifyCode(byte[] secretKey, string code);

        // VerifyCode ile aynı kontrol; geçerliyse kodun eşleştiği zaman adımını da verir. Çağıran taraf adımı saklayıp
        // aynı (ya da daha eski) adımın kodunu ikinci kez kabul etmez (tekrar kullanım koruması, RFC 6238 §5.2).
        bool TryVerifyCode(byte[] secretKey, string code, out long timeStep);
    }
}
