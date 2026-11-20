using ECommerce.Application.Exceptions;
using ECommerce.Application.Features.Order.Constants;
using ECommerce.Application.Features.Order.Rules;
using ECommerce.Application.Services.PaymentService;

namespace ECommerce.Application.UnitTests.Features.Order.Rules
{
    // BR-15: Ödemesi reddedilen sipariş oluşmaz (docs/domain.md, spec 0008).
    public class OrderBusinessRulesTests
    {
        private readonly OrderBusinessRules orderBusinessRules = new();

        [Fact]
        public void PaymentShouldBeApproved_Declined_ThrowsPaymentDeclined()
        {
            BusinessException exception = Assert.Throws<BusinessException>(
                () => orderBusinessRules.PaymentShouldBeApproved(new PaymentResult(IsApproved: false)));

            Assert.Equal(OrderConstants.PaymentDeclined, exception.Message);
        }

        [Fact]
        public void PaymentShouldBeApproved_Approved_DoesNotThrow()
        {
            Exception? exception = Record.Exception(
                () => orderBusinessRules.PaymentShouldBeApproved(new PaymentResult(IsApproved: true)));

            Assert.Null(exception);
        }
    }
}
