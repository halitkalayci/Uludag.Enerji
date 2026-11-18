using System.Security.Cryptography;
using System.Text;

namespace Uludag.Core.Security.Encryption
{
    // Simetrik şifreleme -> şifreleyen ve çözen AYNI anahtarı kullanır.
    // Algoritma: AES-256-GCM -> hem gizlilik hem bütünlük sağlar (veri değiştirilirse çözme hata verir).
    // Hashing'den farkı: hash tek yönlüdür (geri alınamaz), şifrelenmiş veri anahtarla geri çözülür.
    public static class SymmetricEncryptionHelper
    {
        private const int KeySize = 32;   // 256 bit anahtar
        private const int NonceSize = 12; // 96 bit nonce (GCM için önerilen boyut)
        private const int TagSize = 16;   // 128 bit doğrulama etiketi

        // Anahtar -> HashingHelper'daki salt gibi rastgele üretilir. Kaybedilirse veri geri çözülemez.
        public static void CreateKey(out byte[] key)
        {
            key = RandomNumberGenerator.GetBytes(KeySize);
        }

        // Çıktı: nonce + tag + şifreli veri -> tek byte[] (DB'de tek kolonda saklanabilir).
        public static byte[] Encrypt(string plainText, byte[] key)
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

            // Nonce -> her şifrelemede yeni üretilir (salt gibi): aynı veri + aynı anahtar => her seferinde farklı çıktı.
            byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);
            byte[] cipherBytes = new byte[plainBytes.Length];
            byte[] tag = new byte[TagSize];

            using AesGcm aes = new(key, TagSize);
            aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

            return [.. nonce, .. tag, .. cipherBytes];
        }

        public static string Decrypt(byte[] encryptedData, byte[] key)
        {
            // Encrypt'in ürettiği paketi parçalarına ayır: nonce + tag + şifreli veri.
            ReadOnlySpan<byte> data = encryptedData;
            ReadOnlySpan<byte> nonce = data[..NonceSize];
            ReadOnlySpan<byte> tag = data.Slice(NonceSize, TagSize);
            ReadOnlySpan<byte> cipherBytes = data[(NonceSize + TagSize)..];
            byte[] plainBytes = new byte[cipherBytes.Length];

            using AesGcm aes = new(key, TagSize);
            aes.Decrypt(nonce, cipherBytes, tag, plainBytes); // Anahtar yanlış / veri değiştirilmiş -> AuthenticationTagMismatchException.

            return Encoding.UTF8.GetString(plainBytes);
        }
    }
}
