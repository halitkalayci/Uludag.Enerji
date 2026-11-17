using ECommerce.Application.Features.Product.Commands.Create;
using Uludag.Core.Mediation;
using Uludag.Core.Mediation.Behaviors;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Mediator + Application katmanındaki tüm handler'lar.
builder.Services.AddMediator(typeof(CreateProductCommand).Assembly);
// auth
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
// auth
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
