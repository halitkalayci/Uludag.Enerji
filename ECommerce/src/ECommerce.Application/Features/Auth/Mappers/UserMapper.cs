using ECommerce.Application.Features.Auth.Commands.Login;
using ECommerce.Application.Features.Auth.Commands.Register;
using Uludag.Core.Security.JWT;

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

        // Core'un AccessToken'ı doğrudan dışarı açılmaz; API'nin kendi response modeline çevrilir.
        public static LoginResponse MapAccessTokenToLoginResponse(AccessToken accessToken)
        {
            return new LoginResponse
            {
                AccessToken = accessToken.Token,
                Expiration = accessToken.Expiration
            };
        }
    }
}
