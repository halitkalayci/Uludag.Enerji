using ECommerce.Application.Exceptions;
using ECommerce.Application.Features.Auth.Constants;
using ECommerce.Application.Services.Repositories;
using System.Diagnostics.CodeAnalysis;
using System.Security.Authentication;
using Uludag.Core.Security.Hashing;
using Uludag.Core.Security.JWT;
using Uludag.Core.Security.MultiFactor;
using Uludag.Core.Security.MultiFactor.Otp;

namespace ECommerce.Application.Features.Auth.Rules
{
    // Kimlik doğrulama (Auth) ile ilgili tüm iş kuralları burada yazılır.
    // Handler'lar if-else ile durum kontrolü yapmaz; bu metotları çağırır.
    // Kural ihlal edilirse metot istisna fırlatır, edilmezse sessizce döner.
    // Otomatik DI kaydı: BaseBusinessRules'tan türediği için AddSubClassesOfType ile Scoped eklenir.
    public class AuthBusinessRules : BaseBusinessRules
    {
        private readonly IUserRepository userRepository;
        private readonly IUserOperationClaimRepository userOperationClaimRepository;
        private readonly IOtpAuthenticatorRepository otpAuthenticatorRepository;
        private readonly IOtpAuthenticatorHelper otpAuthenticatorHelper; // Uludag.Core -> kodun nasıl doğrulandığını (OtpNet) kural bilmez.
        private readonly IMultiFactorSessionHelper multiFactorSessionHelper;
        private readonly IRefreshTokenRepository refreshTokenRepository;
        public AuthBusinessRules(
            IUserRepository userRepository,
            IUserOperationClaimRepository userOperationClaimRepository,
            IOtpAuthenticatorRepository otpAuthenticatorRepository,
            IOtpAuthenticatorHelper otpAuthenticatorHelper,
            IMultiFactorSessionHelper multiFactorSessionHelper,
            IRefreshTokenRepository refreshTokenRepository)
        {
            this.userRepository = userRepository;
            this.userOperationClaimRepository = userOperationClaimRepository;
            this.otpAuthenticatorRepository = otpAuthenticatorRepository;
            this.otpAuthenticatorHelper = otpAuthenticatorHelper;
            this.multiFactorSessionHelper = multiFactorSessionHelper;
            this.refreshTokenRepository = refreshTokenRepository;
        }

        // Rol atanacak kullanıcı var olmalı -> yoksa 404.
        public async Task UserShouldExist(Guid userId, CancellationToken cancellationToken = default)
        {
            Domain.Entities.User? user = await userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null)
                throw new NotFoundException(AuthConstants.UserNotFound);
        }

        // Atanacak claim sistemde tanımlı olmalı -> yoksa 404.
        // [NotNull]: Metot hata fırlatmadan dönerse derleyici operationClaim'in artık null olmadığını bilir.
        public void OperationClaimShouldExist([NotNull] Domain.Entities.OperationClaim? operationClaim)
        {
            if (operationClaim is null)
                throw new NotFoundException(AuthConstants.OperationClaimNotFound);
        }

        // Aynı claim bir kullanıcıya iki kez atanamaz -> 400.
        // (Veritabanındaki unique index de bunu engeller; bu kural kullanıcıya anlamlı mesaj vermek için.)
        public async Task UserCannotHaveOperationClaim(Guid userId, Guid operationClaimId, CancellationToken cancellationToken = default)
        {
            if (await userOperationClaimRepository.AnyAsync(userId, operationClaimId, cancellationToken))
                throw new BusinessException(AuthConstants.UserAlreadyHasOperationClaim);
        }

        // Aynı e-posta ile ikinci bir kullanıcı kaydedilemez. -> Sisteme (DB'ye) bakmak gerekiyor, iş kuralı.
        public async Task UserEmailCannotExist(string email, CancellationToken cancellationToken = default)
        {
            Domain.Entities.User? userWithSameEmail = await userRepository.GetByEmailAsync(email, cancellationToken);
            if (userWithSameEmail != null)
                throw new BusinessException(AuthConstants.UserEmailAlreadyExists);
        }

        // Girişte kullanıcı bulunamazsa 404 değil 400 döner (BusinessException):
        // "e-posta yok" ile "şifre yanlış" aynı mesajı verir, böylece kayıtlı e-postalar dışarıdan tespit edilemez.
        // Zamanlama saldırısına karşı: kullanıcı yoksa da sahte bir hash doğrulaması yapılır.
        // Böylece "e-posta yok" yanıtı, "şifre yanlış" yanıtından daha hızlı dönmez; ms farkından e-postanın kayıtlı olup olmadığı anlaşılamaz.
        // [NotNull]: Metot hata fırlatmadan dönerse derleyici user'ın artık null olmadığını bilir.
        public void UserShouldExistWhenLoggingIn([NotNull] Domain.Entities.User? user, string password)
        {
            if (user is null)
            {
                // Fake hash verify -> sonucu önemsiz, yalnızca gerçek doğrulamayla aynı süreyi harcamak için.
                HashingHelper.VerifyPasswordHash(password, fakeCredentials.Hash, fakeCredentials.Salt);
                throw new BusinessException(AuthConstants.InvalidEmailOrPassword);
            }
        }

        // Sahte doğrulamada kullanılan hash + salt. Gerçek kayıtla aynı boyutta olması için HashingHelper ile üretilir.
        // static: uygulama ömrü boyunca bir kez üretilir, her istekte yeniden hesaplanmaz.
        private static readonly (byte[] Hash, byte[] Salt) fakeCredentials = CreateFakeCredentials();

        private static (byte[] Hash, byte[] Salt) CreateFakeCredentials()
        {
            HashingHelper.CreatePasswordHash(Guid.NewGuid().ToString(), out byte[] passwordHash, out byte[] passwordSalt);
            return (passwordHash, passwordSalt);
        }

        // Şifre doğrulaması -> tamamen Uludag.Core'daki HashingHelper üzerinden.
        public void PasswordShouldMatch(Domain.Entities.User user, string password)
        {
            if (!HashingHelper.VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
                throw new BusinessException(AuthConstants.InvalidEmailOrPassword);
        }

        // ---- Multi-Factor Authentication (OTP) ----

        // MFA token'ı login'de verilir ve kısa ömürlüdür. Yanlış / süresi dolmuş / kullanılmışsa -> 400, yeniden login.
        // [NotNull]: Metot hata fırlatmadan dönerse derleyici session'ın artık null olmadığını bilir.
        public void MultiFactorSessionShouldExist([NotNull] MultiFactorSession? session)
        {
            if (session is null)
                throw new BusinessException(AuthConstants.InvalidMultiFactorToken);
        }

        // Oturumdaki kullanıcı bu arada silinmiş olabilir -> 404.
        public void UserShouldExistWhenSelected([NotNull] Domain.Entities.User? user)
        {
            if (user is null)
                throw new NotFoundException(AuthConstants.UserNotFound);
        }

        // MFA'sı aktif kullanıcı için yeniden kurulum (yeni gizli anahtar) yapılamaz -> 400.
        // Aksi halde şifreyi ele geçiren biri kendi telefonunu ekleyip MFA'yı devre dışı bırakabilirdi.
        public async Task MultiFactorCannotBeEnabled(Guid userId, CancellationToken cancellationToken = default)
        {
            if (await otpAuthenticatorRepository.IsVerifiedAsync(userId, cancellationToken))
                throw new BusinessException(AuthConstants.MultiFactorAlreadyEnabled);
        }

        // Kod doğrulamadan önce kurulum başlatılmış olmalı (gizli anahtar üretilmiş) -> yoksa 400.
        public void OtpAuthenticatorShouldExist([NotNull] Domain.Entities.OtpAuthenticator? otpAuthenticator)
        {
            if (otpAuthenticator is null)
                throw new BusinessException(AuthConstants.OtpAuthenticatorNotSetUp);
        }

        // Kaba kuvvet koruması: her doğrulama denemesi bir hak harcar (kod doğru olsa da).
        // Kod kontrolünden ÖNCE çağrılmalı; sayaç atomik olduğu için aynı anda gönderilen istekler de sınırı aşamaz.
        public async Task VerificationAttemptShouldBeAllowed(Guid userId)
        {
            if (!await multiFactorSessionHelper.TryConsumeVerificationAttemptAsync(userId))
                throw new BusinessException(AuthConstants.TooManyAuthenticatorAttempts);
        }

        // Kod, authenticator uygulamasının şu an ürettiği kodla eşleşmeli -> değilse 400.
        // Geçerliyse kodun eşleştiği zaman adımını döner (tekrar kullanım kontrolü için).
        public long AuthenticatorCodeShouldBeValid(byte[] secretKey, string authenticatorCode)
        {
            if (!otpAuthenticatorHelper.TryVerifyCode(secretKey, authenticatorCode, out long timeStep))
                throw new BusinessException(AuthConstants.InvalidAuthenticatorCode);
            return timeStep;
        }

        // Başarıyla kullanılmış bir kod (ya da daha eski bir adımın kodu) ikinci kez kabul edilmez (RFC 6238 §5.2).
        // Kontrol ve kayıt tek atomik adımdır: eşzamanlı iki istekten yalnızca biri geçer.
        public async Task AuthenticatorCodeShouldNotBeReused(Guid userId, long timeStep, CancellationToken cancellationToken = default)
        {
            if (!await otpAuthenticatorRepository.TryUseTimeStepAsync(userId, timeStep, cancellationToken))
                throw new BusinessException(AuthConstants.AuthenticatorCodeAlreadyUsed);
        }

        // ---- Refresh Token ----

        // Gönderilen token'ın (özetinin) kaydı olmalı -> yoksa 400.
        // [NotNull]: Metot hata fırlatmadan dönerse derleyici refreshToken'ın artık null olmadığını bilir.
        public void RefreshTokenShouldExist([NotNull] Domain.Entities.RefreshToken? refreshToken)
        {
            if (refreshToken is null)
                throw new BusinessException(AuthConstants.InvalidRefreshToken);
        }

        // Revoke edilmiş (kullanılmış / iptal edilmiş) bir token tekrar gelirse token çalınmış sayılır:
        // ya saldırgan ya da gerçek kullanıcı eski bir kopyayı kullanıyor, hangisi olduğu bilinemez.
        // Güvenli taraf: aynı girişten türeyen aktif token da revoke edilir, iki taraf da yeniden login olmak zorunda kalır -> 400.
        public async Task RefreshTokenShouldNotBeRevoked(Domain.Entities.RefreshToken refreshToken, CancellationToken cancellationToken = default)
        {
            if (refreshToken.RevokedAt is not null)
            {
                await refreshTokenRepository.RevokeActiveFamilyAsync(
                    refreshToken.FamilyId,
                    Domain.Entities.RefreshTokenRevocationReason.ReuseDetected,
                    cancellationToken);
                throw new BusinessException(AuthConstants.RefreshTokenReuseDetected);
            }
        }

        // Süresi (3 gün) dolmuş token kullanılamaz / revoke edilemez -> 400, yeniden login.
        public void RefreshTokenShouldNotBeExpired(Domain.Entities.RefreshToken refreshToken)
        {
            if (refreshToken.ExpiresAt <= DateTime.UtcNow)
                throw new BusinessException(AuthConstants.RefreshTokenExpired);
        }

        // Token'ı atomik olarak revoke eder; yalnızca hâlâ aktifse başarılı olur.
        // Aynı token ile eşzamanlı iki istek gelirse yalnızca biri geçer, diğeri 400 alır -> bir token'dan iki çift üretilemez.
        // Önce revoke, sonra yeni token (VerificationAttemptShouldBeAllowed ile aynı yaklaşım: önce hak harcanır).
        public async Task RefreshTokenShouldBeRevoked(
            Guid refreshTokenId,
            Domain.Entities.RefreshTokenRevocationReason reason,
            Guid? replacedByTokenId = null,
            CancellationToken cancellationToken = default)
        {
            if (!await refreshTokenRepository.TryRevokeActiveAsync(refreshTokenId, reason, replacedByTokenId, cancellationToken))
                throw new BusinessException(AuthConstants.InvalidRefreshToken);
        }

        // ---- Logout ----

        // İsteğin token oturumu olmalı -> yoksa 401.
        // AuthenticationBehavior bunu zaten kontrol eder; bu kural, kontrolden sonra oturumun silindiği
        // (ör. aynı anda gönderilmiş ikinci bir logout) durumu için.
        // [NotNull]: Metot hata fırlatmadan dönerse derleyici tokenSession'ın artık null olmadığını bilir.
        public void TokenSessionShouldExist([NotNull] TokenSession? tokenSession)
        {
            if (tokenSession is null)
                throw new AuthenticationException(AuthConstants.InvalidTokenSession);
        }
    }
}
