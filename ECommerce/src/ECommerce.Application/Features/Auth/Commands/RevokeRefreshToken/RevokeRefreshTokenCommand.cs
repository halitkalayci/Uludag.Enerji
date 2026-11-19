using ECommerce.Application.Features.Auth.Rules;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;
using Uludag.Core.Security.JWT;

namespace ECommerce.Application.Features.Auth.Commands.RevokeRefreshToken
{
    // Refresh token'ı iptal eder (ör. çıkış). Bundan sonra bu token ile yeni JWT alınamaz.
    // Sonuçlar:
    //   başarılı -> 204
    //   token yok / zaten revoke edilmiş (eşzamanlı istek) -> 400 InvalidRefreshToken
    //   süresi dolmuş -> 400 RefreshTokenExpired
    //   kullanılmış (rotasyonla yerine yenisi verilmiş) token geldi -> ailenin aktif token'ı da revoke edilir, 400 RefreshTokenReuseDetected
    // IAllowAnonymousRequest -> JWT'nin süresi dolmuş olabilir; refresh token'a sahip olmak onu iptal etmeye yeter.
    public class RevokeRefreshTokenCommand : IRequest, IAllowAnonymousRequest
    {
        public string RefreshToken { get; set; } = null!;

        public class RevokeRefreshTokenCommandHandler : IRequestHandler<RevokeRefreshTokenCommand>
        {
            // Dependency Injection
            private readonly IRefreshTokenRepository refreshTokenRepository;
            private readonly AuthBusinessRules authBusinessRules;
            private readonly ITokenHelper tokenHelper;
            public RevokeRefreshTokenCommandHandler(
                IRefreshTokenRepository refreshTokenRepository,
                AuthBusinessRules authBusinessRules,
                ITokenHelper tokenHelper)
            {
                this.refreshTokenRepository = refreshTokenRepository;
                this.authBusinessRules = authBusinessRules;
                this.tokenHelper = tokenHelper;
            }

            public async Task Handle(RevokeRefreshTokenCommand request, CancellationToken cancellationToken)
            {
                // Veritabanı -> düz token saklanmadığı için özetiyle aranır.
                string tokenHash = tokenHelper.HashRefreshToken(request.RefreshToken);
                Domain.Entities.RefreshToken? refreshToken = await refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

                // İş kuralları
                authBusinessRules.RefreshTokenShouldExist(refreshToken);
                await authBusinessRules.RefreshTokenShouldNotBeRevoked(refreshToken, cancellationToken);
                authBusinessRules.RefreshTokenShouldNotBeExpired(refreshToken);

                // Revoke -> atomik; yalnızca hâlâ aktifse.
                await authBusinessRules.RefreshTokenShouldBeRevoked(
                    refreshToken.Id,
                    Domain.Entities.RefreshTokenRevocationReason.RevokedByUser,
                    cancellationToken: cancellationToken);
            }
        }
    }
}
