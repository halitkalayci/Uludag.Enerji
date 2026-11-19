using ECommerce.Application.Features.Auth.Mappers;
using ECommerce.Application.Features.Auth.Rules;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;
using Uludag.Core.Security.JWT;

namespace ECommerce.Application.Features.Auth.Commands.RefreshToken
{
    // Refresh token ile yeni bir JWT + refresh token çifti alınır (rotasyon). MFA tekrar sorulmaz.
    // Her refresh token TEK kullanımlıktır: kullanılınca revoke edilir (Rotated) ve yerine aynı aileden yeni bir token üretilir.
    // Sonuçlar:
    //   başarılı -> 200 + yeni çift
    //   token yok / zaten kullanılmış (eşzamanlı istek) -> 400 InvalidRefreshToken
    //   süresi dolmuş -> 400 RefreshTokenExpired
    //   revoke edilmiş token tekrar geldi -> ailenin aktif token'ı da revoke edilir, 400 RefreshTokenReuseDetected
    // IAllowAnonymousRequest -> JWT'nin süresi dolmuş olabilir; kimliği refresh token'ın kendisi kanıtlar.
    public class RefreshTokenCommand : IRequest<RefreshTokenResponse>, IAllowAnonymousRequest
    {
        public string RefreshToken { get; set; } = null!;

        public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResponse>
        {
            // Dependency Injection
            private readonly IUserRepository userRepository;
            private readonly IUserOperationClaimRepository userOperationClaimRepository;
            private readonly IRefreshTokenRepository refreshTokenRepository;
            private readonly AuthBusinessRules authBusinessRules;
            private readonly ITokenHelper tokenHelper; // Uludag.Core -> token'ların nasıl üretildiğini handler bilmez.
            public RefreshTokenCommandHandler(
                IUserRepository userRepository,
                IUserOperationClaimRepository userOperationClaimRepository,
                IRefreshTokenRepository refreshTokenRepository,
                AuthBusinessRules authBusinessRules,
                ITokenHelper tokenHelper)
            {
                this.userRepository = userRepository;
                this.userOperationClaimRepository = userOperationClaimRepository;
                this.refreshTokenRepository = refreshTokenRepository;
                this.authBusinessRules = authBusinessRules;
                this.tokenHelper = tokenHelper;
            }

            public async Task<RefreshTokenResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
            {
                // Veritabanı -> düz token saklanmadığı için özetiyle aranır.
                string tokenHash = tokenHelper.HashRefreshToken(request.RefreshToken);
                Domain.Entities.RefreshToken? currentRefreshToken = await refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

                // İş kuralları
                authBusinessRules.RefreshTokenShouldExist(currentRefreshToken);
                await authBusinessRules.RefreshTokenShouldNotBeRevoked(currentRefreshToken, cancellationToken);
                authBusinessRules.RefreshTokenShouldNotBeExpired(currentRefreshToken);

                Domain.Entities.User? user = await userRepository.GetByIdAsync(currentRefreshToken.UserId, cancellationToken);
                authBusinessRules.UserShouldExistWhenSelected(user);
                 
                // Yeni refresh token -> aynı ailede kalır.
                IssuedRefreshToken issuedRefreshToken = tokenHelper.CreateRefreshToken();
                Domain.Entities.RefreshToken nextRefreshToken = RefreshTokenMapper.MapToRefreshToken(user.Id, currentRefreshToken.FamilyId, issuedRefreshToken);

                // Önce eski token atomik olarak revoke edilir (eşzamanlı ikinci istek burada 400 alır), sonra yenisi yazılır.
                await authBusinessRules.RefreshTokenShouldBeRevoked(
                    currentRefreshToken.Id,
                    Domain.Entities.RefreshTokenRevocationReason.Rotated,
                    nextRefreshToken.Id,
                    cancellationToken);
                await refreshTokenRepository.AddAsync(nextRefreshToken, cancellationToken);

                // Yetkiler DB'den güncel haliyle okunur; yeni JWT oturumu Redis'e yazılır.
                List<string> operationClaims = await userOperationClaimRepository.GetOperationClaimNamesByUserIdAsync(user.Id, cancellationToken);
                AccessToken accessToken = await tokenHelper.CreateTokenAsync(user.Id, user.Email, operationClaims);

                // Mapping
                return UserMapper.MapToRefreshTokenResponse(accessToken, issuedRefreshToken);
            }
        }
    }
}
