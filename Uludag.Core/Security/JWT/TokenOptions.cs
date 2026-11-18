namespace Uludag.Core.Security.JWT
{
    // appsettings'teki "TokenOptions" bölümünün C# karşılığı.
    // Token'ı üreten (JwtHelper) ve doğrulayan (WebAPI'deki JwtBearer) aynı ayarları kullanır.
    public class TokenOptions
    {
        public string Issuer { get; set; } = null!;   // Token'ı kim üretti?
        public string Audience { get; set; } = null!; // Token kimin için üretildi?
        public int AccessTokenExpiration { get; set; } // Dakika cinsinden geçerlilik süresi.

        // İmza anahtarı. HmacSha256 için en az 32 byte (32 ASCII karakter) olmalı.
        // Koda / repoya yazılmaz: ortam değişkeni (TokenOptions__SecurityKey) veya secret store'dan gelir.
        public string SecurityKey { get; set; } = null!;
    }
}
