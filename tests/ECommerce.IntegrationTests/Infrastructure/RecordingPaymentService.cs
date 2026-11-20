using System.Collections.Concurrent;
using ECommerce.Application.Services.PaymentService;

namespace ECommerce.IntegrationTests.Infrastructure
{
    // Ödeme sağlayıcısının stub'ı (spec 0008): sonucu test belirler, gelen her isteği kaydeder.
    // "Ödemeye ne gitti?" (AC-3) ve "ödeme hiç denendi mi?" (AC-4, AC-7) sorularını gözlemlenebilir yapar.
    public sealed class RecordingPaymentService(bool approve) : IPaymentService
    {
        private readonly ConcurrentQueue<PaymentRequest> requests = new();

        public IReadOnlyList<PaymentRequest> Requests => requests.ToArray();

        public Task<PaymentResult> PayAsync(PaymentRequest request, CancellationToken cancellationToken = default)
        {
            requests.Enqueue(request);
            return Task.FromResult(new PaymentResult(approve));
        }
    }
}
