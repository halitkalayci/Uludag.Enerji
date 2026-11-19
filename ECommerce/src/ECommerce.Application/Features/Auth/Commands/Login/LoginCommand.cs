using ECommerce.Application.Features.Auth.Mappers;
using ECommerce.Application.Features.Auth.Rules;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;
using Uludag.Core.Security.MultiFactor;

namespace ECommerce.Application.Features.Auth.Commands.Login
{
    // Kullanıcı girişinin 1. adımı: e-posta + şifre doğrulanır. JWT DÖNMEZ.
    // Başarılıysa kısa ömürlü bir MFA token'ı ve sıradaki adım döner (bkz. LoginResponse):
    //   MFA kurulmamış (ilk giriş) -> SetupRequired : /api/auth/mfa/setup + /api/auth/mfa/verify
    //   MFA kurulu                 -> CodeRequired  : /api/auth/mfa/verify
    // E-posta yok veya şifre yanlış -> BusinessException -> 400.
    // IAllowAnonymousRequest -> token almak için giriş yapılır; AuthenticationBehavior bu isteği kontrol etmez.
    public class LoginCommand : IRequest<LoginResponse>, IAllowAnonymousRequest
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public int Test { get; set; }

        public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
        {
            // Dependency Injection
            private readonly IUserRepository userRepository;
            private readonly IOtpAuthenticatorRepository otpAuthenticatorRepository;
            private readonly AuthBusinessRules authBusinessRules;
            private readonly IMultiFactorSessionHelper multiFactorSessionHelper; // Uludag.Core -> MFA oturumunun nerede tutulduğunu (Redis) handler bilmez.
            public LoginCommandHandler(
                IUserRepository userRepository,
                IOtpAuthenticatorRepository otpAuthenticatorRepository,
                AuthBusinessRules authBusinessRules,
                IMultiFactorSessionHelper multiFactorSessionHelper)
            {
                this.userRepository = userRepository;
                this.otpAuthenticatorRepository = otpAuthenticatorRepository;
                this.authBusinessRules = authBusinessRules;
                this.multiFactorSessionHelper = multiFactorSessionHelper;
            }

            public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
            {
                var result = 15 / request.Test;

                // Veritabanı
                Domain.Entities.User? user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);

                // İş kuralları
                authBusinessRules.UserShouldExistWhenLoggingIn(user, request.Password);
                authBusinessRules.PasswordShouldMatch(user, request.Password);

                // MFA aktif mi? -> yalnızca sıradaki adımı (kurulum mu, kod mu) belirler; iki durumda da JWT verilmez.
                bool isMultiFactorEnabled = await otpAuthenticatorRepository.IsVerifiedAsync(user.Id, cancellationToken);

                // Şifre doğru -> MFA oturumu (Redis). Token yalnızca MFA isteklerinde geçerlidir, hiçbir API'ye yetki vermez.
                MultiFactorSession multiFactorSession = await multiFactorSessionHelper.CreateSessionAsync(user.Id);

                // Mapping
                return UserMapper.MapMultiFactorSessionToLoginResponse(multiFactorSession, isMultiFactorEnabled);
            }
        }
    }
}
