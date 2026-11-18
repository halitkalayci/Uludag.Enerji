using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace Uludag.Core.Security.JWT
{
    // ITokenHelper'ın JWT ile yazılmış hali.
    // İmza: HmacSha256 (simetrik) -> aynı SecurityKey ile hem imzalanır hem doğrulanır.
    public class JwtHelper : ITokenHelper
    {
        private readonly TokenOptions tokenOptions;
        private readonly JsonWebTokenHandler tokenHandler = new();
        private readonly TokenValidationParameters validationParameters;
        public JwtHelper(TokenOptions tokenOptions)
        {
            this.tokenOptions = tokenOptions;
            validationParameters = TokenValidationParametersHelper.Create(tokenOptions);
        }

        public AccessToken CreateToken(Guid userId, string email)
        {
            DateTime now = DateTime.UtcNow;
            DateTime expiration = now.AddMinutes(tokenOptions.AccessTokenExpiration);

            SecurityTokenDescriptor tokenDescriptor = new()
            {
                Issuer = tokenOptions.Issuer,
                Audience = tokenOptions.Audience,
                IssuedAt = now,
                NotBefore = now,
                Expires = expiration,
                // Claim -> token'ın içinde taşınan bilgi. Şifre, hash gibi gizli veriler ASLA eklenmez
                // (JWT şifreli değil, imzalıdır: içeriği herkes okuyabilir, ama değiştiremez).
                Subject = new ClaimsIdentity(
                [
                    new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                    new Claim(JwtRegisteredClaimNames.Email, email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // Her token'a özel kimlik.
                ]),
                SigningCredentials = new SigningCredentials(
                    SecurityKeyHelper.CreateSecurityKey(tokenOptions.SecurityKey),
                    SecurityAlgorithms.HmacSha256)
            };

            return new AccessToken
            {
                Token = tokenHandler.CreateToken(tokenDescriptor),
                Expiration = expiration
            };
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            // İmza, issuer, audience ve süre kontrolü. Hata fırlatmaz; sonucu IsValid ile döner.
            TokenValidationResult result = await tokenHandler.ValidateTokenAsync(token, validationParameters);
            return result.IsValid;
        }
    }
}
