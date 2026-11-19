using Microsoft.AspNetCore.Mvc;
using Uludag.Core.RateLimiting;

namespace ECommerce.WebAPI.Middlewares
{
    // Her HTTP isteğini istemci IP'sinin kovasından geçirir (Leaky Bucket -> Uludag.Core).
    // Muafiyet yok: Swagger ve /openapi dahil her istek bir damla sayılır.
    //   Kovada yer var -> istek devam eder.
    //   Kova dolu      -> 429 Too Many Requests + Retry-After (saniye) + ProblemDetails.
    // Pipeline'ın en başında çalışır: reddedilen istek hiçbir iş (DB, token doğrulama vb.) yaptırmaz.
    public class LeakyBucketRateLimitingMiddleware
    {
        // IP alınamayan istekler (ör. test sunucusu) tek bir ortak kovayı paylaşır.
        private const string UnknownClientKey = "unknown";

        private readonly RequestDelegate next;
        private readonly ILeakyBucketRateLimiter leakyBucketRateLimiter;
        public LeakyBucketRateLimitingMiddleware(RequestDelegate next, ILeakyBucketRateLimiter leakyBucketRateLimiter)
        {
            this.next = next;
            this.leakyBucketRateLimiter = leakyBucketRateLimiter;
        }

        public async Task InvokeAsync(HttpContext context)
        {
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
    }
}
