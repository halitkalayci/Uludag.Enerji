using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Uludag.Core.Caching;

namespace Uludag.Core.Security.JWT
{
    // ITokenHelper'ın JWT ile yazılmış hali.
    // İmza: HmacSha256 (simetrik) -> aynı SecurityKey ile hem imzalanır hem doğrulanır.
    //
    // Token içeriği bilerek boş tutulur: yalnızca "sid" (oturum anahtarı) + standart iss / aud / exp / iat / nbf.
    // Kullanıcı bilgileri ICacheService'te (Redis) üç anahtarda durur:
    //   token-session:{sid}             -> UserId, Email, Expiration. Her login'de yeni bir tane; TTL = token süresi.
    //   user-operation-claims:{userId}  -> Yetki adları. Kullanıcı başına TEK kayıt; tüm oturumları bunu okur.
    //   user-token-sessions:{userId}    -> Kullanıcının oturum anahtarları (sid) kümesi (Redis SET). Logout'ta
    //                                      kullanıcının bütün oturumlarını bulup silmek için; TTL = en son açılan oturumun süresi.
    // Yetkiler oturuma kopyalanmadığı için, bu tek kayıt güncellenince (RefreshOperationClaimsAsync)
    // kullanıcının bütün açık token'ları bir sonraki istekte yeni yetkilerle çalışır.
    public class JwtHelper : ITokenHelper
    {
        // JWT'de oturum anahtarını taşıyan claim.
        public const string SessionKeyClaimType = JwtRegisteredClaimNames.Sid;

        private const string SessionCacheKeyPrefix = "token-session:";
        private const string OperationClaimsCacheKeyPrefix = "user-operation-claims:";
        private const string UserSessionsCacheKeyPrefix = "user-token-sessions:";

        // Refresh token: 512 bit rastgele -> tahmin edilemez.
        private const int RefreshTokenByteLength = 64;

        // Cache'teki oturum kaydı. Yetkiler burada değil, kullanıcının ortak kaydında durur.
        private sealed record StoredTokenSession(Guid UserId, string Email, DateTime Expiration);

        private readonly TokenOptions tokenOptions;
        private readonly ICacheService cacheService;
        private readonly JsonWebTokenHandler tokenHandler = new();
        private readonly TokenValidationParameters validationParameters;
        public JwtHelper(TokenOptions tokenOptions, ICacheService cacheService)
        {
            this.tokenOptions = tokenOptions;
            this.cacheService = cacheService;
            validationParameters = TokenValidationParametersHelper.Create(tokenOptions);
        }

        public async Task<AccessToken> CreateTokenAsync(Guid userId, string email, IEnumerable<string> operationClaims)
        {
            DateTime now = DateTime.UtcNow;
            DateTime expiration = now.AddMinutes(tokenOptions.AccessTokenExpiration);
            TimeSpan lifetime = expiration - now;

            // Tahmin edilemez anahtar (Guid v4 -> 122 bit rastgele). Token imzalı olduğundan dışarıdan da değiştirilemez.
            string sessionKey = Guid.NewGuid().ToString("N");

            // Yetkiler DB'den yeni okunmuş, en güncel hal -> kullanıcının ortak kaydının üzerine yazılır.
            // TTL en son açılan oturumun süresi kadar: tüm token'lar aynı süreyle üretildiği için en son biten oturum budur,
            // yani ortak kayıt hiçbir aktif oturumdan önce silinmez.
            await cacheService.SetAsync(BuildOperationClaimsCacheKey(userId), operationClaims.ToList(), lifetime);

            // Oturum, kaydından ÖNCE kullanıcının oturum kümesine eklenir: logout'un bulamayacağı bir oturum hiç oluşmaz.
            // (Ters sırada, iki yazma arasında hata olursa kümede olmayan, yani logout ile kapatılamayan bir oturum kalırdı.
            // Bu sırada en kötü ihtimal, kümede karşılığı olmayan bir anahtardır; silinmesi zararsızdır.)
            await cacheService.AddToSetAsync(BuildUserSessionsCacheKey(userId), sessionKey, lifetime);

            // Önce cache yazılır: token istemciye ulaştığında oturum kesinlikle hazırdır.
            await cacheService.SetAsync(BuildSessionCacheKey(sessionKey), new StoredTokenSession(userId, email, expiration), lifetime);

            SecurityTokenDescriptor tokenDescriptor = new()
            {
                Issuer = tokenOptions.Issuer,
                Audience = tokenOptions.Audience,
                IssuedAt = now,
                NotBefore = now,
                Expires = expiration,
                // Yalnızca anahtar. E-posta, roller vb. token'a yazılmaz (JWT şifreli değil, imzalıdır: içeriği herkes okuyabilir).
                Subject = new ClaimsIdentity([new Claim(SessionKeyClaimType, sessionKey)]),
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
            return await GetTokenSessionAsync(token) is not null;
        }

        public async Task<TokenSession?> GetTokenSessionAsync(string token)
        {
            // Doğrulanmamış token'a güvenilmez: önce imza / issuer / audience / süre kontrolü, sonra cache.
            // ValidateTokenAsync hata fırlatmaz; sonucu IsValid ile döner.
            TokenValidationResult result = await tokenHandler.ValidateTokenAsync(token, validationParameters);
            if (!result.IsValid)
                return null;

            string? sessionKey = result.ClaimsIdentity.FindFirst(SessionKeyClaimType)?.Value;
            if (string.IsNullOrEmpty(sessionKey))
                return null;

            // Oturum yoksa (süresi dolmuş / silinmiş) token geçersizdir.
            StoredTokenSession? storedSession = await cacheService.GetAsync<StoredTokenSession>(BuildSessionCacheKey(sessionKey));
            if (storedSession is null)
                return null;

            // Yetkiler her istekte kullanıcının ortak kaydından okunur -> atama / kaldırma anında yansır.
            // Kayıt yoksa yetki yok sayılır (güvenli taraf: 403, asla fazladan yetki değil).
            List<string> operationClaims = await cacheService.GetAsync<List<string>>(BuildOperationClaimsCacheKey(storedSession.UserId)) ?? [];

            return new TokenSession
            {
                UserId = storedSession.UserId,
                Email = storedSession.Email,
                OperationClaims = operationClaims,
                Expiration = storedSession.Expiration
            };
        }

        public async Task<IReadOnlyCollection<string>> GetOperationClaimsAsync(string token)
        {
            TokenSession? tokenSession = await GetTokenSessionAsync(token);
            return tokenSession?.OperationClaims ?? [];
        }

        public async Task RefreshOperationClaimsAsync(Guid userId, IEnumerable<string> operationClaims)
        {
            // Yalnızca kayıt varsa (kullanıcının aktif oturumu varsa) ve kalan süresine dokunmadan güncellenir.
            // Oturumu olmayan kullanıcı için kayıt açılmaz; yetkiler bir sonraki login'de zaten yazılır.
            await cacheService.SetIfExistsAsync(BuildOperationClaimsCacheKey(userId), operationClaims.ToList());
        }

        public async Task RevokeAllTokenSessionsAsync(Guid userId)
        {
            // Küme atomik olarak okunup silinir -> aynı anda açılan yeni bir oturum ya burada silinir
            // ya da yeni oluşan kümede kalır (logout'tan sonra yapılmış geçerli bir giriş sayılır).
            IReadOnlyCollection<string> sessionKeys = await cacheService.PopAllFromSetAsync(BuildUserSessionsCacheKey(userId));

            // Oturumlar + kullanıcının yetki kaydı tek DEL ile silinir. Süresi zaten dolmuş oturumların anahtarları
            // da kümede olabilir; olmayan anahtarı silmek zararsızdır.
            await cacheService.RemoveAsync(
                sessionKeys
                    .Select(BuildSessionCacheKey)
                    .Append(BuildOperationClaimsCacheKey(userId)));
        }

        public IssuedRefreshToken CreateRefreshToken()
        {
            // Hex -> URL / JSON içinde kaçış gerektirmez.
            string refreshToken = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(RefreshTokenByteLength));

            return new IssuedRefreshToken
            {
                Token = refreshToken,
                TokenHash = HashRefreshToken(refreshToken),
                Expiration = DateTime.UtcNow.AddDays(tokenOptions.RefreshTokenExpiration)
            };
        }

        public string HashRefreshToken(string refreshToken)
        {
            // Salt'sız SHA-256 yeterli: token 512 bit rastgele olduğu için sözlük / rainbow table saldırısı anlamsızdır,
            // ve salt olmadığı için DB'de doğrudan özetle (unique index üzerinden) aranabilir.
            // Şifre hash'i (HashingHelper) burada kullanılmaz: yavaş ve salt'lı olduğundan özetle arama yapılamaz.
            return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken ?? string.Empty)));
        }

        private static string BuildSessionCacheKey(string sessionKey) => SessionCacheKeyPrefix + sessionKey;

        private static string BuildOperationClaimsCacheKey(Guid userId) => OperationClaimsCacheKeyPrefix + userId;

        private static string BuildUserSessionsCacheKey(Guid userId) => UserSessionsCacheKeyPrefix + userId;
    }
}
