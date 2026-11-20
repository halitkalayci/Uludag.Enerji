using ECommerce.Application.Services.PaymentService;
using ECommerce.Domain.ValueObjects;
using ECommerce.Infrastructure.Payment;

namespace ECommerce.Infrastructure.UnitTests.Payment
{
    // Fake ödeme sağlayıcısı (spec 0008, AC-5 / K-5).
    public class FakePaymentServiceTests
    {
        // Sabit tohum: sonuç her koşuda aynı (docs/testing.md, Determinism).
        private const int Seed = 20261120;
        private const int Attempts = 10_000;

        private static readonly PaymentRequest Request = new(Guid.NewGuid(), new Money(100m, "TRY"));

        [Fact]
        public void Options_Default_DeclineRateIsQuarter()
        {
            Assert.Equal(0.25, new FakePaymentOptions().DeclineRate);
        }

        [Fact]
        public async Task PayAsync_DefaultRate_DeclinesAboutQuarter()
        {
            FakePaymentService service = new(new FakePaymentOptions(), new Random(Seed));

            int declined = 0;
            for (int i = 0; i < Attempts; i++)
                if (!(await service.PayAsync(Request, TestContext.Current.CancellationToken)).IsApproved)
                    declined++;

            // Spec 0008, AC-5: 10.000 denemede %23–%27 (G-2: ≈ ±4,6 standart sapma).
            double declineRatio = (double)declined / Attempts;
            Assert.InRange(declineRatio, 0.23, 0.27);
        }

        [Fact]
        public async Task PayAsync_RateZero_AlwaysApproves()
        {
            FakePaymentService service = new(new FakePaymentOptions { DeclineRate = 0 }, new Random(Seed));

            for (int i = 0; i < Attempts; i++)
                Assert.True((await service.PayAsync(Request, TestContext.Current.CancellationToken)).IsApproved);
        }

        [Fact]
        public async Task PayAsync_RateOne_AlwaysDeclines()
        {
            FakePaymentService service = new(new FakePaymentOptions { DeclineRate = 1 }, new Random(Seed));

            for (int i = 0; i < Attempts; i++)
                Assert.False((await service.PayAsync(Request, TestContext.Current.CancellationToken)).IsApproved);
        }
    }
}
