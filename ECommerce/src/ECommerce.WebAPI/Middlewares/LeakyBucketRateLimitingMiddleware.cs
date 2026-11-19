using Microsoft.AspNetCore.Mvc;
using Uludag.Core.RateLimiting;

namespace ECommerce.WebAPI.Middlewares
{
    // Her HTTP isteğini istemci IP'sinin kovasından geçirir (Leaky Bucket -> Uludag.Core).
    // Whitelist'teki yollar (RateLimitOptions:Whitelist) rate limiting dışındadır: kovaya sayılmaz, hiç reddedilmez.
    // Eşleşme önek usulü, segment sınırına bakılır, harf duyarsız: "/swagger" -> /swagger/index.html muaf, /swaggerX değil.
    // Diğer tüm istekler:
    //   Kovada yer var -> istek devam eder.
    //   Kova dolu      -> 429 Too Many Requests + Retry-After (saniye) + ProblemDetails.
    // Pipeline'ın en başında çalışır: reddedilen istek hiçbir iş (DB, token doğrulama vb.) yaptırmaz.
    public class LeakyBucketRateLimitingMiddleware
    {
        // IP alınamayan istekler (ör. test sunucusu) tek bir ortak kovayı paylaşır.
        private const string UnknownClientKey = "unknown";

        private readonly RequestDelegate next;
        private readonly ILeakyBucketRateLimiter leakyBucketRateLimiter;
        private readonly PathString[] whitelist;
        public LeakyBucketRateLimitingMiddleware(RequestDelegate next, ILeakyBucketRateLimiter leakyBucketRateLimiter, LeakyBucketOptions leakyBucketOptions)
        {
            this.next = next;
            this.leakyBucketRateLimiter = leakyBucketRateLimiter;
            // Sondaki "/" atılır: "/swagger/" de "/swagger" gibi davranır (aksi halde /swagger yolunun kendisi eşleşmezdi).
            // Değerlerin "/" ile başladığı AddLeakyBucketRateLimiter'da açılışta kontrol edilir.
            whitelist = leakyBucketOptions.Whitelist
                .Select(path => path.TrimEnd('/'))
                .Select(path => new PathString(path.Length == 0 ? "/" : path))
                .ToArray();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Request.Path, Kestrel tarafından ".." / "." segmentlerinden arındırılmış haldedir:
            // "/swagger/../api/products" gibi bir yol whitelist'e takılmaz, "/api/products" olarak limitlenir.
            if (IsWhitelisted(context.Request.Path))
            {
                await next(context);
                return;
            }

            // Bağlantının gerçek adresi. X-Forwarded-For okunmaz: istemci bu başlığı değiştirerek limiti atlatabilirdi.
            // İleride reverse proxy eklenirse UseForwardedHeaders + bilinen proxy listesi gerekir.
            string clientKey = context.Connection.RemoteIpAddress?.ToString() ?? UnknownClientKey;

            RateLimitResult rateLimitResult = await leakyBucketRateLimiter.TryAcquireAsync(clientKey);
            if (rateLimitResult.IsAllowed)
            {
                await next(context);
                return;
            }

            // Yukarı yuvarlanır ve en az 1 sn: istemci önerilen sürede tekrar denediğinde damla sızmış olur.
            int retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(rateLimitResult.RetryAfter.TotalSeconds));

            ProblemDetails problem = new()
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Too Many Requests",
                Detail = $"Çok fazla istek gönderildi. {retryAfterSeconds} saniye sonra tekrar deneyin.",
                Instance = context.Request.Path
            };

            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers.RetryAfter = retryAfterSeconds.ToString();
            await context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
        }

        // StartsWithSegments: önek eşleşmesi + segment sınırı ("/swagger" -> "/swagger/x" evet, "/swaggerX" hayır).
        private bool IsWhitelisted(PathString path) =>
            whitelist.Any(whitelistedPath => path.StartsWithSegments(whitelistedPath, StringComparison.OrdinalIgnoreCase));
    }
}
