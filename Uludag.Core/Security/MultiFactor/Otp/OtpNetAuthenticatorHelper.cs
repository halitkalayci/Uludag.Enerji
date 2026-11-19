using OtpNet;

namespace Uludag.Core.Security.MultiFactor.Otp
{
    // IOtpAuthenticatorHelper'ın OtpNet (Otp.NET paketi) ile yazılmış hali.
    // Ayarlar Google / Microsoft Authenticator'ın varsayılanlarıdır: SHA1, 6 hane, 30 saniye.
    // Bunlar değiştirilirse bazı uygulamalar otpauth adresindeki ayarı yok sayıp yanlış kod üretir -> sabit tutulur.
    public class OtpNetAuthenticatorHelper : IOtpAuthenticatorHelper
    {
        // RFC 4226: SHA1 için önerilen anahtar boyutu 160 bit (20 byte).
        private const int SecretKeyLength = 20;
        private const int CodeLength = 6;
        private const int StepSeconds = 30;

        private readonly MultiFactorOptions multiFactorOptions;
        public OtpNetAuthenticatorHelper(MultiFactorOptions multiFactorOptions)
        {
            this.multiFactorOptions = multiFactorOptions;
        }

        public byte[] GenerateSecretKey()
        {
            // Kriptografik olarak güvenli rastgele üretici kullanır (RandomNumberGenerator).
            return KeyGeneration.GenerateRandomKey(SecretKeyLength);
        }

        public string ConvertSecretKeyToString(byte[] secretKey)
        {
            return Base32Encoding.ToString(secretKey);
        }

        public string CreateKeyUri(string accountName, byte[] secretKey)
        {
            // Ör: otpauth://totp/Uludag%20ECommerce:user%40mail.com?secret=...&issuer=Uludag%20ECommerce&algorithm=SHA1&digits=6&period=30
            return new OtpUri(OtpType.Totp, secretKey, accountName, multiFactorOptions.Issuer,
                OtpHashMode.Sha1, CodeLength, StepSeconds).ToString();
        }

        public bool VerifyCode(byte[] secretKey, string code)
        {
            // Boş / yanlış uzunlukta / rakam dışı karakter içeren kod hesaplamaya bile girmez.
            if (string.IsNullOrWhiteSpace(code))
                return false;

            // Bazı uygulamalar kodu "123 456" gösterir; kullanıcı boşlukla yapıştırabilir.
            string normalizedCode = code.Replace(" ", string.Empty);
            if (normalizedCode.Length != CodeLength || !normalizedCode.All(char.IsAsciiDigit))
                return false;

            Totp totp = new(secretKey, StepSeconds, OtpHashMode.Sha1, CodeLength);
            VerificationWindow window = new(previous: multiFactorOptions.VerificationWindow, future: multiFactorOptions.VerificationWindow);

            // Karşılaştırma OtpNet içinde sabit zamanlıdır (zamanlama saldırısına karşı).
            return totp.VerifyTotp(normalizedCode, out _, window);
        }
    }
}
