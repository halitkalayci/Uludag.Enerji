using ECommerce.Application.Features.Auth.Mappers;
using ECommerce.Application.Features.Auth.Rules;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;
using Uludag.Core.Security.JWT;
using Uludag.Core.Security.MultiFactor;

namespace ECommerce.Application.Features.Auth.Commands.VerifyOtpAuthenticator
{
    // Girişin son adımı: MFA token'ı + authenticator kodu doğrulanır, JWT döner.
    // İki durumda da aynı istek kullanılır:
    //   - İlk kurulum (SetupRequired): /api/auth/mfa/setup'tan sonra uygulamanın ilk kodu -> MFA aktif olur.
    //   - Sonraki girişler (CodeRequired): uygulamanın o anki kodu.
    // Sonuçlar: başarılı -> 200 + JWT, token geçersiz / kurulum yok / kod yanlış / deneme hakkı bitti -> 400.
    // MFA token'ı tek kullanımlıktır: başarılı doğrulamadan sonra silinir.
    // IAllowAnonymousRequest -> kullanıcının henüz JWT'si yok; kimliğini login'de aldığı MFA token'ı kanıtlar.
    public class VerifyOtpAuthenticatorCommand : IRequest<VerifyOtpAuthenticatorResponse>, IAllowAnonymousRequest
    {
        public string MultiFactorToken { get; set; } = null!;
        public string AuthenticatorCode { get; set; } = null!;

        public class VerifyOtpAuthenticatorCommandHandler : IRequestHandler<VerifyOtpAuthenticatorCommand, VerifyOtpAuthenticatorResponse>
        {
            // Dependency Injection
            private readonly IUserRepository userRepository;
            private readonly IOtpAuthenticatorRepository otpAuthenticatorRepository;
            private readonly IUserOperationClaimRepository userOperationClaimRepository;
            private readonly AuthBusinessRules authBusinessRules;
            private readonly IMultiFactorSessionHelper multiFactorSessionHelper;
            private readonly ITokenHelper tokenHelper; // Uludag.Core -> token'ın nasıl üretildiğini handler bilmez.
            public VerifyOtpAuthenticatorCommandHandler(
                IUserRepository userRepository,
                IOtpAuthenticatorRepository otpAuthenticatorRepository,
                IUserOperationClaimRepository userOperationClaimRepository,
                AuthBusinessRules authBusinessRules,
                IMultiFactorSessionHelper multiFactorSessionHelper,
                ITokenHelper tokenHelper)
            {
                this.userRepository = userRepository;
                this.otpAuthenticatorRepository = otpAuthenticatorRepository;
                this.userOperationClaimRepository = userOperationClaimRepository;
                this.authBusinessRules = authBusinessRules;
                this.multiFactorSessionHelper = multiFactorSessionHelper;
                this.tokenHelper = tokenHelper;
            }

            public async Task<VerifyOtpAuthenticatorResponse> Handle(VerifyOtpAuthenticatorCommand request, CancellationToken cancellationToken)
            {
                // İş kuralları
                MultiFactorSession? multiFactorSession = await multiFactorSessionHelper.GetSessionAsync(request.MultiFactorToken);
                authBusinessRules.MultiFactorSessionShouldExist(multiFactorSession);

                Domain.Entities.OtpAuthenticator? otpAuthenticator = await otpAuthenticatorRepository.GetByUserIdAsync(multiFactorSession.UserId, cancellationToken);
                authBusinessRules.OtpAuthenticatorShouldExist(otpAuthenticator);

                // Önce hak harcanır, sonra kod kontrol edilir (kaba kuvvet koruması).
                await authBusinessRules.VerificationAttemptShouldBeAllowed(multiFactorSession.UserId);
                authBusinessRules.AuthenticatorCodeShouldBeValid(otpAuthenticator.SecretKey, request.AuthenticatorCode);

                Domain.Entities.User? user = await userRepository.GetByIdAsync(multiFactorSession.UserId, cancellationToken);
                authBusinessRules.UserShouldExistWhenSelected(user);

                // Veritabanı -> ilk kurulumsa MFA artık aktif; zaten aktifse değişiklik olmaz.
                await otpAuthenticatorRepository.MarkAsVerifiedAsync(user.Id, cancellationToken);

                // MFA token'ı tek kullanımlık; deneme sayacı başarılı girişte sıfırlanır.
                await multiFactorSessionHelper.RemoveSessionAsync(multiFactorSession.Token);
                await multiFactorSessionHelper.ResetVerificationAttemptsAsync(user.Id);

                // Yetkiler -> kullanıcının Redis'teki yetki kaydına yazılır; AuthorizationBehavior her istekte bunları oradan okur.
                List<string> operationClaims = await userOperationClaimRepository.GetOperationClaimNamesByUserIdAsync(user.Id, cancellationToken);

                // Token -> id, e-posta ve yetkiler Redis'e yazılır; JWT yalnızca o kaydın anahtarını taşır.
                AccessToken accessToken = await tokenHelper.CreateTokenAsync(user.Id, user.Email, operationClaims);

                // Mapping
                return UserMapper.MapAccessTokenToVerifyOtpAuthenticatorResponse(accessToken);
            }
        }
    }
}
