using ECommerce.Application.Services.PaymentService;

namespace ECommerce.Infrastructure.Payment
{
    // Gerçek bir sağlayıcıya gitmeyen ödeme servisi (spec 0008): her deneme bağımsızdır ve
    // DeclineRate olasılığıyla reddedilir. Para çekilmez, kart bilgisi istenmez.
    public class FakePaymentService : IPaymentService
    {
        private readonly FakePaymentOptions options;
        private readonly Random random;

        // DI: Random.Shared (thread-safe). Testler tohumlu bir Random verir (docs/testing.md, Determinism).
        public FakePaymentService(FakePaymentOptions options) : this(options, Random.Shared)
        {
        }

        public FakePaymentService(FakePaymentOptions options, Random random)
        {
            this.options = options;
            this.random = random;
        }

        public Task<PaymentResult> PayAsync(PaymentRequest request, CancellationToken cancellationToken = default)
        {
            // NextDouble() [0, 1) aralığında: oran 0 iken hiç, 1 iken her zaman ret.
            bool isDeclined = random.NextDouble() < options.DeclineRate;
            return Task.FromResult(new PaymentResult(IsApproved: !isDeclined));
        }
    }
}
