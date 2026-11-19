namespace Uludag.Core.Security.JWT
{
    // Token üretiminin ve doğrulamasının sözleşmesi. Uygulama katmanı yalnızca bu arayüzü bilir;
    // token'ın nasıl üretildiğini (JWT, imza algoritması, kütüphane) ve bilgilerin nerede saklandığını (Redis / bellek) bilmez.
    // Core, ECommerce'in User entity'sini tanımaz; bu yüzden oturuma girecek bilgiler tek tek verilir.
    public interface ITokenHelper
    {
        // Kullanıcı bilgileri (id, e-posta, yetkiler) cache'e bir oturum olarak yazılır,
        // dönen JWT yalnızca bu oturumun anahtarını taşır.
        // operationClaims -> kullanıcının yetki adları (ör. "Product.Read").
        Task<AccessToken> CreateTokenAsync(Guid userId, string email, IEnumerable<string> operationClaims);

        // Token bizim tarafımızdan üretilmiş (imza, issuer, audience doğru), süresi geçmemiş
        // ve cache'te oturumu duruyorsa true.
        Task<bool> ValidateTokenAsync(string token);

        // Token'ı doğrular ve cache'teki oturumu döner. Token geçersizse veya oturum yoksa null.
        Task<TokenSession?> GetTokenSessionAsync(string token);

        // Token'ı doğrular ve oturumdaki operation claim adlarını döner. Token / oturum geçersizse boş koleksiyon.
        Task<IReadOnlyCollection<string>> GetOperationClaimsAsync(string token);

        // Kullanıcının yetkileri değiştiğinde çağrılır: aktif oturumlarının hepsi, yeniden login gerekmeden,
        // bir sonraki istekte yeni yetkilerle çalışır. operationClaims -> kullanıcının GÜNCEL yetki listesinin tamamı.
        // Kullanıcının aktif oturumu yoksa hiçbir şey yapmaz (yetkiler zaten bir sonraki login'de yazılır).
        Task RefreshOperationClaimsAsync(Guid userId, IEnumerable<string> operationClaims);

        // Çıkış (logout): kullanıcının cache'teki TÜM token oturumlarını (her cihaz / her giriş) ve yetki kaydını siler.
        // Bu çağrıdan sonra kullanıcının daha önce aldığı bütün JWT'ler, imzası ve süresi geçerli olsa bile 401 alır.
        // Refresh token'lar uygulamanın veritabanındadır; onları iptal etmek uygulamanın işidir.
        Task RevokeAllTokenSessionsAsync(Guid userId);

        // Yeni bir refresh token üretir (tahmin edilemez rastgele değer + SHA-256 özeti + bitiş zamanı).
        // Saklama, rotasyon ve iptal (revoke) uygulamanın işidir; Core yalnızca değeri üretir.
        IssuedRefreshToken CreateRefreshToken();

        // İstemciden gelen düz refresh token'ın veritabanında aranacak özetini üretir.
        string HashRefreshToken(string refreshToken);
    }
}
