using Microsoft.AspNetCore.Http;
using Uludag.Core.Security.Authorization;
using Uludag.Core.Security.Constants;
using Uludag.Core.Security.JWT;

namespace Uludag.Core.Mediation.Behaviors
{
    // Kimlik doğrulandıktan sonra (AuthenticationBehavior) "bu kullanıcı bu işlemi yapabilir mi?" sorusunu cevaplar.
    // Yalnızca ISecuredRequest istekleri kontrol edilir; diğerleri doğrudan geçer.
    // Claim'ler token'ın içinden değil, token'ın işaret ettiği cache (Redis) oturumundan okunur.
    // Kural: kullanıcının oturumundaki claim'lerden biri
    //   - "Admin" ise (global yönetici) veya
    //   - isteğin Roles listesinde birebir (metinsel, büyük/küçük harf duyarlı) varsa
    // istek geçer; yoksa AuthorizationException (WebAPI'de 403'e çevrilir).
    // Örnek: CreateProductCommand.Roles = [Product.Admin, Product.Write, Product.Create]
    //   -> "Product.Write" claim'i olan kullanıcı ürün ekleyebilir, yalnızca "Product.Read" olan ekleyemez.
    // Kayıt: AuthenticationBehavior'dan SONRA eklenmeli (önce kim olduğun, sonra neye yetkin olduğu).
    public class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ITokenHelper tokenHelper;
        public AuthorizationBehavior(IHttpContextAccessor httpContextAccessor, ITokenHelper tokenHelper)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.tokenHelper = tokenHelper;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            // Yetki gerektirmeyen istek -> kontrol yapılmadan devam.
            if (request is not ISecuredRequest securedRequest)
                return await next();

            // Token yoksa / geçersizse / oturumu yoksa claim listesi boş gelir -> yetki yok.
            string? token = httpContextAccessor.HttpContext.GetBearerToken();
            IReadOnlyCollection<string> userClaims = token is null
                ? []
                : await tokenHelper.GetOperationClaimsAsync(token);

            bool isAuthorized = userClaims.Contains(GeneralOperationClaims.Admin, StringComparer.Ordinal)
                || userClaims.Intersect(securedRequest.Roles, StringComparer.Ordinal).Any();

            if (!isAuthorized)
                throw new AuthorizationException("Bu işlem için yetkiniz bulunmamaktadır.");

            return await next();
        }
    }
}
