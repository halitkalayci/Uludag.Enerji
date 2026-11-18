using System.Security.Cryptography;
using System.Text;

namespace Uludag.Core.Security.Encryption
{
    // Asimetrik şifreleme -> anahtar ÇİFTİ kullanılır:
    //   Public key  -> herkesle paylaşılabilir, yalnızca ŞİFRELER.
    //   Private key -> sadece sahibinde kalır, yalnızca ÇÖZER.
    // Algoritma: RSA-2048 + OAEP-SHA256 padding.
    // Sınır: RSA-2048 + OAEP-SHA256 ile tek seferde en fazla 190 byte şifrelenebilir (aşılırsa CryptographicException).
    // Büyük veri için: veriyi SymmetricEncryptionHelper ile şifrele, yalnızca simetrik anahtarı bu helper ile şifrele (hibrit).
    public static class AsymmetricEncryptionHelper
    {
        private const int KeySizeInBits = 2048;
        private static readonly RSAEncryptionPadding Padding = RSAEncryptionPadding.OaepSHA256;

        // HashingHelper.CreatePasswordHash ile aynı kalıp: üretilenler out parametre ile döner.
        // Public key -> SubjectPublicKeyInfo, private key -> PKCS#8 formatında (standart, taşınabilir).
        public static void CreateKeyPair(out byte[] publicKey, out byte[] privateKey)
        {
            using RSA rsa = RSA.Create(KeySizeInBits);

            publicKey = rsa.ExportSubjectPublicKeyInfo();
            privateKey = rsa.ExportPkcs8PrivateKey();
        }

        // Şifreleme -> public key ile.
        public static byte[] Encrypt(string plainText, byte[] publicKey)
        {
            using RSA rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(publicKey, out _);

            return rsa.Encrypt(Encoding.UTF8.GetBytes(plainText), Padding);
        }

        // Çözme -> private key ile. Yanlış anahtarla çözmeye çalışılırsa CryptographicException.
        public static string Decrypt(byte[] encryptedData, byte[] privateKey)
        {
            using RSA rsa = RSA.Create();
            rsa.ImportPkcs8PrivateKey(privateKey, out _);

            return Encoding.UTF8.GetString(rsa.Decrypt(encryptedData, Padding));
        }
    }
}
