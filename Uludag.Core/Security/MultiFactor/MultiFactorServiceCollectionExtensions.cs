using Uludag.Core.Security.MultiFactor;
using Uludag.Core.Security.MultiFactor.Otp;

// Kullanan projeler ekstra using yazmadan services.AddMultiFactorAuthentication(...) diyebilsin diye
// uzantı, DI'ın kendi namespace'inde tanımlanır.
namespace Microsoft.Extensions.DependencyInjection
{
    public static class MultiFactorServiceCollectionExtensions
    {
        // MultiFactorOptions'ı, IOtpAuthenticatorHelper -> OtpNetAuthenticatorHelper ve
        // IMultiFactorSessionHelper -> MultiFactorSessionHelper eşleşmelerini kaydeder.
        // Ayarlar eksik / hatalıysa uygulama ilk login'de değil, açılışta hata verir.
        // MFA oturumları ICacheService'te tutulur -> önce services.AddCacheService(...) çağrılmalıdır.
        public static IServiceCollection AddMultiFactorAuthentication(this IServiceCollection services, MultiFactorOptions multiFactorOptions)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(multiFactorOptions);

            if (string.IsNullOrWhiteSpace(multiFactorOptions.Issuer))
                throw new ArgumentException("MultiFactorOptions.Issuer boş olamaz.", nameof(multiFactorOptions));
            if (multiFactorOptions.SessionExpiration <= 0)
                throw new ArgumentException("MultiFactorOptions.SessionExpiration sıfırdan büyük olmalıdır.", nameof(multiFactorOptions));
            if (multiFactorOptions.VerificationWindow < 0)
                throw new ArgumentException("MultiFactorOptions.VerificationWindow negatif olamaz.", nameof(multiFactorOptions));
            if (multiFactorOptions.MaxVerificationAttempts <= 0)
                throw new ArgumentException("MultiFactorOptions.MaxVerificationAttempts sıfırdan büyük olmalıdır.", nameof(multiFactorOptions));
            if (multiFactorOptions.LockoutDuration <= 0)
                throw new ArgumentException("MultiFactorOptions.LockoutDuration sıfırdan büyük olmalıdır.", nameof(multiFactorOptions));

            // Singleton: ayarlar uygulama boyunca değişmez, helper'lar durum tutmaz (oturumlar ICacheService'te).
            services.AddSingleton(multiFactorOptions);
            services.AddSingleton<IOtpAuthenticatorHelper, OtpNetAuthenticatorHelper>();
            services.AddSingleton<IMultiFactorSessionHelper, MultiFactorSessionHelper>();

            return services;
        }
    }
}
