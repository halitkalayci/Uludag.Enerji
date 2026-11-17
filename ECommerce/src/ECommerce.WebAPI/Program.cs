using ECommerce.Application;
using ECommerce.Persistence;


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

// caching
// Pipeline behavior örneği (sıra önemlidir, ilk eklenen en dışta çalışır):
// builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
