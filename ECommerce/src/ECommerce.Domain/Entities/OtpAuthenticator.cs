namespace ECommerce.Domain.Entities
{
    // Kullanıcının authenticator uygulaması (TOTP) kaydı. User ile bire-bir: her kullanıcının en fazla bir kaydı olur.
    // Kurulum iki adımdır:
    //   1) Gizli anahtar üretilir, kayıt IsVerified = false ile yazılır (QR kullanıcıya gösterilir).
    //   2) Kullanıcı uygulamanın ürettiği ilk kodu doğru girerse IsVerified = true -> MFA aktif.
    // Doğrulanmamış kayıt "yarım kalmış kurulum"dur; MFA aktif sayılmaz ve yeni kurulumda silinip yeniden oluşturulur.
    public class OtpAuthenticator
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }

        // TOTP gizli anahtarı. Şifre gibi hash'lenemez: sunucu her doğrulamada kodu bu anahtardan yeniden üretir.
        public byte[] SecretKey { get; set; } = null!;
        public bool IsVerified { get; set; }

        // Navigation Properties
        public virtual User User { get; set; } = null!;
    }
}
