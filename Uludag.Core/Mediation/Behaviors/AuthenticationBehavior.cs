using Microsoft.AspNetCore.Http;
using System.Security.Authentication;
using Uludag.Core.Security.JWT;

namespace Uludag.Core.Mediation.Behaviors
{
    // Her istek handler'a ulaşmadan önce JWT'yi doğrular.
    // Token "Authorization: Bearer <token>" başlığından okunur ve ITokenHelper ile kontrol edilir:
    //   - Bizim SecurityKey'imizle imzalanmış mı?
    //   - Issuer / Audience bizim mi?
    //   - Süresi geçmemiş mi?
    //   - Token'daki oturum anahtarının (sid) cache'te (Redis) kaydı duruyor mu?
    // Hepsi doğruysa next(), değilse AuthenticationException (WebAPI'de 401'e çevrilir).
    // Giriş gerektirmeyen istekler (Login, Register) IAllowAnonymousRequest ile işaretlenir ve kontrol atlanır.
    // Kayıt: services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthenticationBehavior<,>));
    public class AuthenticationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ITokenHelper tokenHelper;
        public AuthenticationBehavior(IHttpContextAccessor httpContextAccessor, ITokenHelper tokenHelper)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.tokenHelper = tokenHelper;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            // Giriş gerektirmeyen istek -> doğrulama yapılmadan devam.
            if (request is IAllowAnonymousRequest)
                return await next();

            string? token = httpContextAccessor.HttpContext.GetBearerToken();
            if (token is null)
                throw new AuthenticationException("Bu işlem için giriş yapılmalıdır.");

            if (!await tokenHelper.ValidateTokenAsync(token))
                throw new AuthenticationException("Geçersiz veya süresi dolmuş token.");

            return await next();
        }
    }
}
