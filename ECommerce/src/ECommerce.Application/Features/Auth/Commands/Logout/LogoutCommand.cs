using ECommerce.Application.Features.Auth.Rules;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;
using Uludag.Core.Security.JWT;

namespace ECommerce.Application.Features.Auth.Commands.Logout
{
    // Çıkış: kullanıcının TÜM token'ları iptal edilir (her cihaz / her giriş):
    //   - Veritabanı: bütün aktif refresh token'lar revoke edilir (RevocationReason = LoggedOut) -> yeni JWT alınamaz.
    //   - Redis: bütün token oturumları (token-session:*) ve yetki kaydı silinir -> mevcut JWT'ler anında 401 alır.
    // Kullanıcı gövdeden değil, isteğin Bearer token'ının oturumundan alınır (başkası adına çıkış yapılamaz).
    // Sonuçlar:
    //   başarılı -> 204
    //   token yok / geçersiz / oturumu zaten kapanmış -> 401
    // IAllowAnonymousRequest DEĞİL -> AuthenticationBehavior geçerli bir JWT ister.
    // JWT'nin süresi dolmuşsa istemci önce refresh-token ile yeni bir JWT alıp sonra çıkış yapar.
    public class LogoutCommand : IRequest
    {
        public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
        {
            // Dependency Injection
            private readonly IRefreshTokenRepository refreshTokenRepository;
            private readonly AuthBusinessRules authBusinessRules;
            private readonly ITokenHelper tokenHelper;
            private readonly ITokenSessionAccessor tokenSessionAccessor; // Uludag.Core -> "kim giriş yapmış" bilgisinin nereden geldiğini handler bilmez.
            public LogoutCommandHandler(
                IRefreshTokenRepository refreshTokenRepository,
                AuthBusinessRules authBusinessRules,
                ITokenHelper tokenHelper,
                ITokenSessionAccessor tokenSessionAccessor)
            {
                this.refreshTokenRepository = refreshTokenRepository;
                this.authBusinessRules = authBusinessRules;
                this.tokenHelper = tokenHelper;
                this.tokenSessionAccessor = tokenSessionAccessor;
            }

            public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
            {
                // İsteği yapan kullanıcının oturumu (Redis).
                TokenSession? tokenSession = await tokenSessionAccessor.GetCurrentTokenSessionAsync();

                // İş kuralları
                authBusinessRules.TokenSessionShouldExist(tokenSession);

                // Sıra önemli: önce veritabanı, sonra Redis.
                // Redis adımı hata verirse JWT hâlâ geçerlidir ve istemci logout'u tekrar deneyebilir.
                // Ters sırada, Redis silinip DB adımı hata verseydi JWT gitmiş olur, refresh token'lar ise açık kalırdı.
                await refreshTokenRepository.RevokeAllActiveByUserIdAsync(
                    tokenSession.UserId,
                    Domain.Entities.RefreshTokenRevocationReason.LoggedOut,
                    cancellationToken);

                await tokenHelper.RevokeAllTokenSessionsAsync(tokenSession.UserId);
            }
        }
    }
}
