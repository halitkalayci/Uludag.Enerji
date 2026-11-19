using ECommerce.Application.Features.Auth.Commands.SetupOtpAuthenticator;

namespace ECommerce.Application.Features.Auth.Mappers
{
    // Auth feature'ının OtpAuthenticator mapping'leri burada yazılır.
    // Command/Query handler'ları mapping yapmaz; bu metotları çağırır.
    public static class OtpAuthenticatorMapper
    {
        // Yeni kurulum her zaman doğrulanmamış başlar; ilk doğru kod girilince IsVerified = true olur.
        public static Domain.Entities.OtpAuthenticator MapToOtpAuthenticator(Guid userId, byte[] secretKey)
        {
            return new Domain.Entities.OtpAuthenticator
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SecretKey = secretKey,
                IsVerified = false
            };
        }

        public static SetupOtpAuthenticatorResponse MapToSetupOtpAuthenticatorResponse(string secretKey, string qrCodeUri)
        {
            return new SetupOtpAuthenticatorResponse
            {
                SecretKey = secretKey,
                QrCodeUri = qrCodeUri
            };
        }
    }
}
