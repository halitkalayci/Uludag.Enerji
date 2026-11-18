namespace Uludag.Core.Security.JWT
{
    // Token üretiminin ve doğrulamasının sözleşmesi. Uygulama katmanı yalnızca bu arayüzü bilir;
    // token'ın nasıl üretildiğini (JWT, imza algoritması, kütüphane) bilmez.
    // Core, ECommerce'in User entity'sini tanımaz; bu yüzden token'a girecek bilgiler tek tek verilir.
    public interface ITokenHelper
    {
        AccessToken CreateToken(Guid userId, string email);

        // Token bizim tarafımızdan üretilmiş (imza, issuer, audience doğru) ve süresi geçmemişse true.
        Task<bool> ValidateTokenAsync(string token);
    }
}
