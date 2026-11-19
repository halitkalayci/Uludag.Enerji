using ECommerce.Application;
using ECommerce.Persistence;
using ECommerce.Persistence.Seeds;
using ECommerce.WebAPI.Middlewares;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;
using Uludag.Core.Caching.Redis;
using Uludag.Core.RateLimiting;
using Uludag.Core.Security.JWT;
using Uludag.Core.Security.MultiFactor;


// Entrypoint -> Giriş noktası..

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();


// DI Container
// Mediator + Application katmanındaki tüm handler'lar.
builder.Services.AddApplicationServices();
// Bu application katmanının bağımlılıkları..
// Başka bir katman adına karar vermemeli.
builder.Services.AddPersistenceServices();

// Cache -> Uludag.Core (ICacheService).
// "RedisOptions:ConnectionString" doluysa Redis (docker-compose), boşsa / yoksa in-memory.
// Token oturumları (kullanıcı id, e-posta, yetkiler) burada saklanır.
RedisOptions? redisOptions = builder.Configuration.GetSection("RedisOptions").Get<RedisOptions>();
builder.Services.AddCacheService(redisOptions);

// Rate limiting -> Uludag.Core (ILeakyBucketRateLimiter, Leaky Bucket algoritması).
// İstemci IP'si başına bir kova: kapasite dolunca istek 429 ile reddedilir (bkz. LeakyBucketRateLimitingMiddleware).
// Ayarlar appsettings'teki "RateLimitOptions" bölümünden okunur. Kovalar cache ile aynı yerde durur:
// Redis ayarlıysa Redis'te (instance'lar arası paylaşılır), değilse bellekte. AddCacheService'ten sonra çağrılmalıdır.
LeakyBucketOptions leakyBucketOptions = builder.Configuration.GetSection("RateLimitOptions").Get<LeakyBucketOptions>()
    ?? throw new InvalidOperationException("appsettings'te 'RateLimitOptions' bölümü bulunamadı.");
builder.Services.AddLeakyBucketRateLimiter(leakyBucketOptions, redisOptions);

// JWT
// Ayarlar appsettings'teki "TokenOptions" bölümünden okunur. SecurityKey repoda değil;
// Development'ta appsettings.Development.json, diğer ortamlarda TokenOptions__SecurityKey ortam değişkeni.
TokenOptions tokenOptions = builder.Configuration.GetSection("TokenOptions").Get<TokenOptions>()
    ?? throw new InvalidOperationException("appsettings'te 'TokenOptions' bölümü bulunamadı.");

// Token üretimi -> Uludag.Core (ITokenHelper -> JwtHelper).
builder.Services.AddJwtHelper(tokenOptions);

// Multi-Factor Authentication -> Uludag.Core (IOtpAuthenticatorHelper -> OtpNet, IMultiFactorSessionHelper -> cache).
// Login JWT yerine kısa ömürlü bir MFA token'ı döner; JWT authenticator kodu doğrulanınca verilir.
// Ayarlar appsettings'teki "MultiFactorOptions" bölümünden okunur. MFA oturumları da ICacheService'te tutulur.
MultiFactorOptions multiFactorOptions = builder.Configuration.GetSection("MultiFactorOptions").Get<MultiFactorOptions>()
    ?? throw new InvalidOperationException("appsettings'te 'MultiFactorOptions' bölümü bulunamadı.");
builder.Services.AddMultiFactorAuthentication(multiFactorOptions);

// Token doğrulama -> gelen isteklerdeki "Authorization: Bearer <token>" başlığını kontrol eder.
// Üretimle aynı TokenOptions + aynı SecurityKeyHelper kullanılır.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // AuthenticationBehavior ile aynı kurallar (Uludag.Core).
        options.TokenValidationParameters = TokenValidationParametersHelper.Create(tokenOptions);

        // Token yalnızca oturum anahtarı taşır. İmza doğrulandıktan sonra kullanıcı bilgileri Redis'ten alınır
        // ve HttpContext.User bunlarla kurulur. Oturum yoksa (süresi dolmuş / silinmiş) istek doğrulanmamış sayılır.
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                ITokenHelper tokenHelper = context.HttpContext.RequestServices.GetRequiredService<ITokenHelper>();
                string token = ((JsonWebToken)context.SecurityToken).EncodedToken;

                TokenSession? tokenSession = await tokenHelper.GetTokenSessionAsync(token);
                if (tokenSession is null)
                {
                    context.Fail("Token oturumu bulunamadı.");
                    return;
                }

                context.Principal = new ClaimsPrincipal(new ClaimsIdentity(tokenSession.ToClaims(), context.Scheme.Name));
            }
        };
    });

// caching
// Pipeline behavior örneği (sıra önemlidir, ilk eklenen en dışta çalışır):
// builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// İlk yönetici -> "AdminSeed" ayarındaki kullanıcı yoksa oluşturulur ve global "Admin" claim'i verilir.
// Ayar tanımlı değilse hiçbir şey yapılmaz.
AdminSeedOptions? adminSeedOptions = builder.Configuration.GetSection("AdminSeed").Get<AdminSeedOptions>();
await app.Services.SeedAdminUserAsync(adminSeedOptions);

// Leaky Bucket rate limiting -> kova doluysa 429. Muafiyet yok (Swagger / openapi dahil).
// Pipeline'ın EN başında: reddedilen istek hiçbir iş (DB, token doğrulama vb.) yaptırmaz.
app.UseMiddleware<LeakyBucketRateLimitingMiddleware>();

// Rules sınıflarının fırlattığı BusinessException / NotFoundException -> 400 / 404.
// Rate limiting'den hemen sonra: sonraki tüm middleware'ler ve controller'lar bunun içinde çalışır.
app.UseMiddleware<BusinessExceptionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // OpenAPI dokümanı -> /openapi/v1.json
    app.MapOpenApi();

    // Swagger arayüzü -> /swagger
    // Dokümanı kendisi üretmez; yukarıdaki /openapi/v1.json'u okuyup gösterir.
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "ECommerce API v1");
    });
}

app.UseHttpsRedirection();

// Sıra önemli: önce kimlik (token geçerli mi, kim?), sonra yetki (bu kişi buna erişebilir mi?).
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
