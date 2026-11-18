using ECommerce.Application.Exceptions;
using ECommerce.Application.Features.Auth.Constants;
using ECommerce.Application.Services.Repositories;
using System.Diagnostics.CodeAnalysis;
using Uludag.Core.Security.Hashing;

namespace ECommerce.Application.Features.Auth.Rules
{
    // Kimlik doğrulama (Auth) ile ilgili tüm iş kuralları burada yazılır.
    // Handler'lar if-else ile durum kontrolü yapmaz; bu metotları çağırır.
    // Kural ihlal edilirse metot istisna fırlatır, edilmezse sessizce döner.
    // Otomatik DI kaydı: BaseBusinessRules'tan türediği için AddSubClassesOfType ile Scoped eklenir.
    public class AuthBusinessRules : BaseBusinessRules
    {
        private readonly IUserRepository userRepository;
        public AuthBusinessRules(IUserRepository userRepository)
        {
            this.userRepository = userRepository;
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
    }
}
