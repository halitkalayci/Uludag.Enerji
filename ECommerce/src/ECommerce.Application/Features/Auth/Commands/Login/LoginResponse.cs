namespace ECommerce.Application.Features.Auth.Commands.Login
{
    // Başarılı girişte dışarıya dönen model.
    public class LoginResponse
    {
        public string AccessToken { get; set; } = null!;
        public DateTime Expiration { get; set; } // UTC
    }
}
