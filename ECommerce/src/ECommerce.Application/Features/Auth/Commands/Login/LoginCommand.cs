using ECommerce.Application.Features.Auth.Mappers;
using ECommerce.Application.Features.Auth.Rules;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;
using Uludag.Core.Security.JWT;

namespace ECommerce.Application.Features.Auth.Commands.Login
{
    // Kullanıcı girişi. E-posta + şifre doğrulanır, başarılıysa JWT döner.
    // Başarılı -> 200 + token, e-posta yok veya şifre yanlış -> BusinessException -> 400.
    // IAllowAnonymousRequest -> token almak için giriş yapılır; AuthenticationBehavior bu isteği kontrol etmez.
    public class LoginCommand : IRequest<LoginResponse>, IAllowAnonymousRequest
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;

        public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
        {
            // Dependency Injection
            private readonly IUserRepository userRepository;
            private readonly AuthBusinessRules authBusinessRules;
            private readonly ITokenHelper tokenHelper; // Uludag.Core -> token'ın nasıl üretildiğini handler bilmez.
            public LoginCommandHandler(IUserRepository userRepository, AuthBusinessRules authBusinessRules, ITokenHelper tokenHelper)
            {
                this.userRepository = userRepository;
                this.authBusinessRules = authBusinessRules;
                this.tokenHelper = tokenHelper;
            }

            public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
            {
                // Veritabanı
                Domain.Entities.User? user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);

                // İş kuralları
                authBusinessRules.UserShouldExistWhenLoggingIn(user, request.Password);
                authBusinessRules.PasswordShouldMatch(user, request.Password);

                // Token
                AccessToken accessToken = tokenHelper.CreateToken(user.Id, user.Email);

                // Mapping
                return UserMapper.MapAccessTokenToLoginResponse(accessToken);
            }
        }
    }
}
