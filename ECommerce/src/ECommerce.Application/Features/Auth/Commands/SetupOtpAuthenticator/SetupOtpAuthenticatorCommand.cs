using ECommerce.Application.Features.Auth.Mappers;
using ECommerce.Application.Features.Auth.Rules;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;
using Uludag.Core.Security.MultiFactor;
using Uludag.Core.Security.MultiFactor.Otp;

namespace ECommerce.Application.Features.Auth.Commands.SetupOtpAuthenticator
{
    // MFA kurulumunun 1. adımı: kullanıcıya yeni bir TOTP gizli anahtarı üretir.
    // Dönen QR adresi (otpauth://...) istemcide QR koda çevrilip authenticator uygulamasıyla okutulur
    // (veya SecretKey elle girilir). Kurulum, uygulamanın ürettiği ilk kod /api/auth/mfa/verify'a gönderilince tamamlanır.
    // Tekrar çağrılırsa yarım kalmış kurulum silinir ve yeni anahtar üretilir (eski QR geçersiz olur).
    // Sonuçlar: başarılı -> 200 + anahtar, MFA token'ı geçersiz -> 400, MFA zaten aktif -> 400, kullanıcı yok -> 404.
    // IAllowAnonymousRequest -> kullanıcının henüz JWT'si yok; kimliğini login'de aldığı MFA token'ı kanıtlar.
    public class SetupOtpAuthenticatorCommand : IRequest<SetupOtpAuthenticatorResponse>, IAllowAnonymousRequest
    {
        public string MultiFactorToken { get; set; } = null!;

        public class SetupOtpAuthenticatorCommandHandler : IRequestHandler<SetupOtpAuthenticatorCommand, SetupOtpAuthenticatorResponse>
        {
            // Dependency Injection
            private readonly IUserRepository userRepository;
            private readonly IOtpAuthenticatorRepository otpAuthenticatorRepository;
            private readonly AuthBusinessRules authBusinessRules;
            private readonly IMultiFactorSessionHelper multiFactorSessionHelper;
            private readonly IOtpAuthenticatorHelper otpAuthenticatorHelper; // Uludag.Core -> anahtarın nasıl üretildiğini (OtpNet) handler bilmez.
            public SetupOtpAuthenticatorCommandHandler(
                IUserRepository userRepository,
                IOtpAuthenticatorRepository otpAuthenticatorRepository,
                AuthBusinessRules authBusinessRules,
                IMultiFactorSessionHelper multiFactorSessionHelper,
                IOtpAuthenticatorHelper otpAuthenticatorHelper)
            {
                this.userRepository = userRepository;
                this.otpAuthenticatorRepository = otpAuthenticatorRepository;
                this.authBusinessRules = authBusinessRules;
                this.multiFactorSessionHelper = multiFactorSessionHelper;
                this.otpAuthenticatorHelper = otpAuthenticatorHelper;
            }

            public async Task<SetupOtpAuthenticatorResponse> Handle(SetupOtpAuthenticatorCommand request, CancellationToken cancellationToken)
            {
                // İş kuralları
                MultiFactorSession? multiFactorSession = await multiFactorSessionHelper.GetSessionAsync(request.MultiFactorToken);
                authBusinessRules.MultiFactorSessionShouldExist(multiFactorSession);

                Domain.Entities.User? user = await userRepository.GetByIdAsync(multiFactorSession.UserId, cancellationToken);
                authBusinessRules.UserShouldExistWhenSelected(user);

                await authBusinessRules.MultiFactorCannotBeEnabled(user.Id, cancellationToken);

                // Yeni gizli anahtar -> Uludag.Core (OtpNet).
                byte[] secretKey = otpAuthenticatorHelper.GenerateSecretKey();

                // Veritabanı -> varsa yarım kalmış kurulum silinir, yenisi doğrulanmamış olarak yazılır.
                await otpAuthenticatorRepository.DeleteUnverifiedByUserIdAsync(user.Id, cancellationToken);

                Domain.Entities.OtpAuthenticator otpAuthenticator = OtpAuthenticatorMapper.MapToOtpAuthenticator(user.Id, secretKey);
                await otpAuthenticatorRepository.AddAsync(otpAuthenticator, cancellationToken);

                // Mapping -> MFA token'ı silinmez: aynı token ile /api/auth/mfa/verify çağrılır.
                return OtpAuthenticatorMapper.MapToSetupOtpAuthenticatorResponse(
                    otpAuthenticatorHelper.ConvertSecretKeyToString(secretKey),
                    otpAuthenticatorHelper.CreateKeyUri(user.Email, secretKey));
            }
        }
    }
}
