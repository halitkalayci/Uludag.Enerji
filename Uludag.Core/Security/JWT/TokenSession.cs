using System.Security.Claims;

namespace Uludag.Core.Security.JWT
{
    // Token'a ait tüm kullanıcı bilgileri. JWT'nin içinde DEĞİL, cache'te (Redis / in-memory) saklanır.
    // JWT yalnızca oturumun anahtarını (sid) taşır -> token'ı çözen biri e-posta / yetki göremez,
    // ve oturum silinince token imzası geçerli olsa bile anında geçersiz olur.
    // Cache'te iki parça halinde durur (bkz. JwtHelper):
    //   token-session:{sid}             -> UserId, Email, Expiration (oturuma özel)
    //   user-operation-claims:{userId}  -> OperationClaims (kullanıcının TÜM oturumları için tek kayıt)
    // Bu sınıf ikisinin birleşimidir; yetki değişikliği kullanıcının bütün oturumlarına anında yansır.
    public class TokenSession
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = null!;
        public List<string> OperationClaims { get; set; } = [];
        public DateTime Expiration { get; set; } // UTC

        // HttpContext.User için claim listesi (WebAPI'deki JwtBearer bu bilgileri token'dan değil buradan alır).
        public IEnumerable<Claim> ToClaims()
        {
            return
            [
                new Claim(ClaimTypes.NameIdentifier, UserId.ToString()),
                new Claim(ClaimTypes.Email, Email),
                .. OperationClaims.Select(operationClaim => new Claim(ClaimTypes.Role, operationClaim))
            ];
        }
    }
}
