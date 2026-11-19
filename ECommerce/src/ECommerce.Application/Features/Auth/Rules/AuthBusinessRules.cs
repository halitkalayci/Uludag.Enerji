using ECommerce.Application.Exceptions;
using ECommerce.Application.Features.Auth.Constants;
using ECommerce.Application.Services.Repositories;
using System.Diagnostics.CodeAnalysis;
using Uludag.Core.Security.Hashing;
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
        public AuthBusinessRules(
            IUserRepository userRepository,
            IUserOperationClaimRepository userOperationClaimRepository,
            IOtpAuthenticatorRepository otpAuthenticatorRepository,
            IOtpAuthenticatorHelper otpAuthenticatorHelper,
            IMultiFactorSessionHelper multiFactorSessionHelper)
        {
            this.userRepository = userRepository;
            this.userOperationClaimRepository = userOperationClaimRepository;
            this.otpAuthenticatorRepository = otpAuthenticatorRepository;
            this.otpAuthenticatorHelper = otpAuthenticatorHelper;
            this.multiFactorSessionHelper = multiFactorSessionHelper;
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
        public void AuthenticatorCodeShouldBeValid(byte[] secretKey, string authenticatorCode)
        {
            if (!otpAuthenticatorHelper.VerifyCode(secretKey, authenticatorCode))
                throw new BusinessException(AuthConstants.InvalidAuthenticatorCode);
        }
    }
}
