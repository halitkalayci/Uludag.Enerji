using Microsoft.IdentityModel.Tokens;

namespace Uludag.Core.Security.JWT
{
    // Token doğrulama kuralları tek yerde tanımlanır.
    // Hem JwtHelper (AuthenticationBehavior) hem WebAPI'deki JwtBearer aynı kuralları kullanır -> iki taraf hiç ayrışmaz.
    public static class TokenValidationParametersHelper
    {
        public static TokenValidationParameters Create(TokenOptions tokenOptions)
        {
            return new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = tokenOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = tokenOptions.Audience,
                ValidateLifetime = true,
                // Hata payı (tolerans) yok: varsayılan 5 dk'dır, yani token süresi dolduktan 5 dk sonrasına kadar geçerli sayılır.
                // Zero -> Expiration anında token geçersizdir.
                ClockSkew = TimeSpan.Zero,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = SecurityKeyHelper.CreateSecurityKey(tokenOptions.SecurityKey)
            };
        }
    }
}
