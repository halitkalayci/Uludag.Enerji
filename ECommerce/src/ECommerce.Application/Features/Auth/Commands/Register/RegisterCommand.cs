using ECommerce.Application.Features.Auth.Mappers;
using ECommerce.Application.Features.Auth.Rules;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;
using Uludag.Core.Security.Hashing;

namespace ECommerce.Application.Features.Auth.Commands.Register
{
    // Yeni kullanıcı kaydı. Şifre veritabanına düz metin olarak yazılmaz;
    // HashingHelper ile hash + salt üretilip yalnızca bunlar saklanır.
    // IAllowAnonymousRequest -> kayıt olmak için giriş gerekmez; AuthenticationBehavior bu isteği kontrol etmez.
    public class RegisterCommand : IRequest, IAllowAnonymousRequest
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;

        public class RegisterCommandHandler : IRequestHandler<RegisterCommand>
        {
            // Dependency Injection
            private readonly IUserRepository userRepository;
            private readonly AuthBusinessRules authBusinessRules;
            public RegisterCommandHandler(IUserRepository userRepository, AuthBusinessRules authBusinessRules)
            {
                this.userRepository = userRepository;
                this.authBusinessRules = authBusinessRules;
            }

            public async Task Handle(RegisterCommand request, CancellationToken cancellationToken)
            {
                // Validasyon (girdi) -> iş kuralı (sistem durumu)
                authBusinessRules.EmailShouldBeValid(request.Email);
                authBusinessRules.PasswordShouldNotBeEmpty(request.Password);
                await authBusinessRules.UserEmailCannotExist(request.Email, cancellationToken);

                // Şifreleme -> tamamen Uludag.Core'daki HashingHelper üzerinden.
                HashingHelper.CreatePasswordHash(request.Password, out byte[] passwordHash, out byte[] passwordSalt);

                // Mapper
                Domain.Entities.User user = UserMapper.MapRegisterCommandToUser(request, passwordHash, passwordSalt);

                // Veritabanı
                await userRepository.AddAsync(user, cancellationToken);
            }
        }
    }
}
