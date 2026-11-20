using ECommerce.Application.Services.PaymentService;
using ECommerce.Infrastructure.Payment;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Infrastructure
{
    // Dış servislerin (şimdilik yalnız fake ödeme) implementasyonlarını sisteme tanıtır.
    public static class InfrastructureServiceRegistration
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, FakePaymentOptions? fakePaymentOptions)
        {
            // Bölüm yoksa varsayılanlar (DeclineRate = 0.25).
            FakePaymentOptions options = fakePaymentOptions ?? new FakePaymentOptions();

            // Hatalı oranla uygulama açılmaz (spec 0008, AC-6). Karşılaştırma NaN'ı da dışarıda bırakır.
            if (!(options.DeclineRate >= 0 && options.DeclineRate <= 1))
                throw new InvalidOperationException(
                    $"appsettings'te '{FakePaymentOptions.SectionName}:{nameof(FakePaymentOptions.DeclineRate)}' 0 ile 1 arasında olmalı (verilen: {options.DeclineRate}).");

            services.AddSingleton(options);
            services.AddSingleton<IPaymentService, FakePaymentService>();
            return services;
        }
    }
}
