namespace Uludag.Core.Security.MultiFactor
{
    // appsettings'teki "MultiFactorOptions" bölümünün C# karşılığı.
    public class MultiFactorOptions
    {
        // Authenticator uygulamasında (Google / Microsoft Authenticator) hesabın üstünde görünen ad.
        // otpauth:// adresine "issuer" olarak yazılır. Ör: "Uludag ECommerce".
        public string Issuer { get; set; } = null!;

        // Şifre doğrulandıktan sonra verilen MFA oturumunun (MultiFactorSession) geçerlilik süresi, dakika.
        // Kullanıcı bu süre içinde kurulumu tamamlamalı / kodu girmelidir. Kurulumda QR okutmak zaman alır -> 10 dk.
        public int SessionExpiration { get; set; } = 10;

        // Kod doğrulamada kabul edilen komşu zaman adımı sayısı (her adım 30 sn).
        // 1 -> bir önceki, şimdiki ve bir sonraki kod kabul edilir (telefon saatindeki küçük kaymalara tolerans).
        public int VerificationWindow { get; set; } = 1;

        // Kullanıcı başına, LockoutDuration penceresi içinde yapılabilecek en fazla kod doğrulama denemesi.
        // 6 haneli kod tahmin edilebilir (10^6 ihtimal) -> deneme sayısı sınırlanmazsa kaba kuvvetle bulunur.
        public int MaxVerificationAttempts { get; set; } = 5;

        // Deneme sayacının penceresi, dakika. Hak biterse kullanıcı bu süre dolana kadar kod doğrulayamaz.
        public int LockoutDuration { get; set; } = 15;
    }
}
