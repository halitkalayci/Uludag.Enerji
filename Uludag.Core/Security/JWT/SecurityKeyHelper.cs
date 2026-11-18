using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Uludag.Core.Security.JWT
{
    // appsettings'teki string anahtarı, imzalama / doğrulamada kullanılan SecurityKey'e çevirir.
    // Token'ı imzalayan (JwtHelper) ve doğrulayan (WebAPI) aynı metodu kullanır -> anahtar iki tarafta hep aynı üretilir.
    public static class SecurityKeyHelper
    {
        public static SecurityKey CreateSecurityKey(string securityKey)
        {
            return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityKey));
        }
    }
}
