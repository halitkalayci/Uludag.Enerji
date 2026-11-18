namespace Uludag.Core.Security.JWT
{
    // Token üretiminin ve doğrulamasının sözleşmesi. Uygulama katmanı yalnızca bu arayüzü bilir;
    // token'ın nasıl üretildiğini (JWT, imza algoritması, kütüphane) bilmez.
    // Core, ECommerce'in User entity'sini tanımaz; bu yüzden token'a girecek bilgiler tek tek verilir.
    public interface ITokenHelper
    {
        // operationClaims -> kullanıcının yetki adları (ör. "Product.Read"); token'a rol claim'i olarak yazılır.
        AccessToken CreateToken(Guid userId, string email, IEnumerable<string> operationClaims);

        // Token bizim tarafımızdan üretilmiş (imza, issuer, audience doğru) ve süresi geçmemişse true.
        Task<bool> ValidateTokenAsync(string token);

        // Token'ı doğrular ve içindeki operation claim adlarını döner. Token geçersizse boş koleksiyon.
        Task<IReadOnlyCollection<string>> GetOperationClaimsAsync(string token);
    }
}
