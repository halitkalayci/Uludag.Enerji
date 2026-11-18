using ECommerce.Application;
using ECommerce.Persistence;
using ECommerce.Persistence.Seeds;
using ECommerce.WebAPI.Middlewares;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Uludag.Core.Security.JWT;


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

// JWT
// Ayarlar appsettings'teki "TokenOptions" bölümünden okunur. SecurityKey repoda değil;
// Development'ta appsettings.Development.json, diğer ortamlarda TokenOptions__SecurityKey ortam değişkeni.
TokenOptions tokenOptions = builder.Configuration.GetSection("TokenOptions").Get<TokenOptions>()
    ?? throw new InvalidOperationException("appsettings'te 'TokenOptions' bölümü bulunamadı.");

// Token üretimi -> Uludag.Core (ITokenHelper -> JwtHelper).
builder.Services.AddJwtHelper(tokenOptions);

// Token doğrulama -> gelen isteklerdeki "Authorization: Bearer <token>" başlığını kontrol eder.
// Üretimle aynı TokenOptions + aynı SecurityKeyHelper kullanılır.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // AuthenticationBehavior ile aynı kurallar (Uludag.Core).
        options.TokenValidationParameters = TokenValidationParametersHelper.Create(tokenOptions);
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

// Rules sınıflarının fırlattığı BusinessException / NotFoundException -> 400 / 404.
// Pipeline'ın en başında: sonraki tüm middleware'ler ve controller'lar bunun içinde çalışır.
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
