using Microsoft.AspNetCore.Http;

namespace Uludag.Core.Security.JWT
{
    // ITokenSessionAccessor'ın HttpContext ile yazılmış hali.
    // Token, AuthenticationBehavior ile aynı kuralla (GetBearerToken) okunur ve ITokenHelper ile doğrulanır.
    // Singleton olabilir: IHttpContextAccessor her çağrıda o anki isteğin HttpContext'ini verir.
    public class HttpContextTokenSessionAccessor : ITokenSessionAccessor
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ITokenHelper tokenHelper;
        public HttpContextTokenSessionAccessor(IHttpContextAccessor httpContextAccessor, ITokenHelper tokenHelper)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.tokenHelper = tokenHelper;
        }

        public async Task<TokenSession?> GetCurrentTokenSessionAsync()
        {
            string? token = httpContextAccessor.HttpContext.GetBearerToken();
            return token is null ? null : await tokenHelper.GetTokenSessionAsync(token);
        }
    }
}
