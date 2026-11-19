namespace Uludag.Core.Security.JWT
{
    // Yeni üretilmiş refresh token.
    //   Token     -> İstemciye YALNIZCA BİR KEZ dönen düz değer. Hiçbir yerde saklanmaz.
    //   TokenHash -> Veritabanına yazılan SHA-256 özeti. DB sızsa bile özetten kullanılabilir token elde edilemez.
    // Core, ECommerce'in RefreshToken entity'sini tanımaz; entity'ye çevirme işi uygulamanın mapper'ındadır.
    public class IssuedRefreshToken
    {
        public string Token { get; set; } = null!;
        public string TokenHash { get; set; } = null!;
        public DateTime Expiration { get; set; } // UTC
    }
}
