using ECommerce.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ECommerce.IntegrationTests.Order
{
    // Hatalı ret oranıyla uygulama açılmaz (spec 0008, AC-6 / K-5).
    [Collection(IntegrationCollection.Name)]
    [Trait("Category", "Integration")]
    public class PaymentConfigurationTests(ApiFixture fixture)
    {
        private const string SettingName = "FakePayment:DeclineRate";

        [Theory]
        [InlineData("-0.1")]
        [InlineData("1.5")]
        public async Task Startup_DeclineRateOutOfRange_FailsWithSettingName(string declineRate)
        {
            await using WebApplicationFactory<Program> factory = fixture.Factory.WithWebHostBuilder(builder =>
                builder.UseSetting(SettingName, declineRate));

            // Services'e erişim uygulamayı ayağa kaldırır; Program.cs açılışta ayarı reddetmeli.
            Exception exception = Assert.ThrowsAny<Exception>(() => factory.Services);

            IEnumerable<string> messages = Flatten(exception).Select(e => e.Message);
            Assert.Contains(messages, message => message.Contains(SettingName, StringComparison.Ordinal));
        }

        private static IEnumerable<Exception> Flatten(Exception exception)
        {
            for (Exception? current = exception; current is not null; current = current.InnerException)
            {
                yield return current;
                if (current is AggregateException aggregate)
                    foreach (Exception inner in aggregate.InnerExceptions.SelectMany(Flatten))
                        yield return inner;
            }
        }
    }
}
