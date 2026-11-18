using Microsoft.AspNetCore.Http;

namespace Uludag.Core.Security.JWT
{
    // "Authorization: Bearer <token>" başlığını okuma işi tek yerde.
    // AuthenticationBehavior ve AuthorizationBehavior aynı token'ı aynı kuralla okur.
    internal static class HttpContextBearerTokenExtensions
    {
        private const string BearerPrefix = "Bearer ";

        // Başlıktan yalnızca token kısmını alır. Başlık yok / biçim yanlışsa null.
        public static string? GetBearerToken(this HttpContext? httpContext)
        {
            string? authorization = httpContext?.Request.Headers.Authorization;
            if (string.IsNullOrWhiteSpace(authorization) ||
                !authorization.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
                return null;

            string token = authorization[BearerPrefix.Length..].Trim();
            return token.Length == 0 ? null : token;
        }
    }
}
