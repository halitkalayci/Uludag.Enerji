using ECommerce.Application.Features.Auth.Commands.Login;
using ECommerce.Application.Features.Auth.Commands.Register;
using ECommerce.Application.Features.Auth.Commands.VerifyOtpAuthenticator;
using Uludag.Core.Security.JWT;
using Uludag.Core.Security.MultiFactor;

namespace ECommerce.Application.Features.Auth.Mappers
{
    // Auth feature'ının User mapping'leri burada yazılır.
    // Command/Query handler'ları mapping yapmaz; bu metotları çağırır.
    public static class UserMapper
    {
        // Şifre düz metin olarak entity'ye geçmez; handler HashingHelper ile üretilen hash + salt'ı verir.
        public static Domain.Entities.User MapRegisterCommandToUser(RegisterCommand command, byte[] passwordHash, byte[] passwordSalt)
        {
            return new Domain.Entities.User
            {
                Email = command.Email,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt
            };
        }

        // Login JWT dönmez; MFA oturumunu ve sıradaki adımı döner.
        // MFA aktif değilse (ilk giriş / kurulum yarım) -> SetupRequired, aktifse -> CodeRequired.
        public static LoginResponse MapMultiFactorSessionToLoginResponse(MultiFactorSession multiFactorSession, bool isMultiFactorEnabled)
        {
            return new LoginResponse
            {
                MultiFactorStatus = isMultiFactorEnabled ? MultiFactorStatus.CodeRequired : MultiFactorStatus.SetupRequired,
                MultiFactorToken = multiFactorSession.Token,
                MultiFactorTokenExpiration = multiFactorSession.Expiration
            };
        }

        // Core'un AccessToken'ı doğrudan dışarı açılmaz; API'nin kendi response modeline çevrilir.
        public static VerifyOtpAuthenticatorResponse MapAccessTokenToVerifyOtpAuthenticatorResponse(AccessToken accessToken)
        {
            return new VerifyOtpAuthenticatorResponse
            {
                AccessToken = accessToken.Token,
                Expiration = accessToken.Expiration
            };
        }
    }
}
