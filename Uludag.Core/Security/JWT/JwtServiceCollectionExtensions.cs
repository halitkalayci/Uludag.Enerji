using Uludag.Core.Security.JWT;

// Kullanan projeler ekstra using yazmadan services.AddJwtHelper(...) diyebilsin diye
// uzantı, DI'ın kendi namespace'inde tanımlanır.
namespace Microsoft.Extensions.DependencyInjection
{
    public static class JwtServiceCollectionExtensions
    {
        // TokenOptions'ı ve ITokenHelper -> JwtHelper eşleşmesini kaydeder.
        // Ayarlar eksik / hatalıysa uygulama ilk login'de değil, açılışta hata verir.
        public static IServiceCollection AddJwtHelper(this IServiceCollection services, TokenOptions tokenOptions)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(tokenOptions);

            if (string.IsNullOrWhiteSpace(tokenOptions.SecurityKey) || tokenOptions.SecurityKey.Length < 32)
                throw new ArgumentException("TokenOptions.SecurityKey en az 32 karakter olmalıdır.", nameof(tokenOptions));
            if (tokenOptions.AccessTokenExpiration <= 0)
                throw new ArgumentException("TokenOptions.AccessTokenExpiration sıfırdan büyük olmalıdır.", nameof(tokenOptions));

            // Singleton: ayarlar uygulama boyunca değişmez, JwtHelper durum tutmaz.
            services.AddSingleton(tokenOptions);
            services.AddSingleton<ITokenHelper, JwtHelper>();

            // AuthenticationBehavior, isteğin Authorization başlığını bununla okur.
            services.AddHttpContextAccessor();

            return services;
        }
    }
}
