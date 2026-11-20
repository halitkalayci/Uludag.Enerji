using System.Net.Http.Json;
using ECommerce.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ECommerce.IntegrationTests.Order
{
    // Ödeme yalnız sipariş oluştururken denenir (spec 0008, AC-7 / R-5).
    [Collection(IntegrationCollection.Name)]
    [Trait("Category", "Integration")]
    public class OrderPaymentScopeTests(ApiFixture fixture)
    {
        private readonly OrderArrange arrange = new(fixture);

        [Fact]
        public async Task OrderAndItemOperations_PaymentAlwaysDeclined_SucceedWithoutPayment()
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            RecordingPaymentService payment = new(approve: false);
            await using WebApplicationFactory<Program> factory = arrange.FactoryWithPayment(payment);
            (Guid userId, HttpClient client) = await arrange.CreateCustomerAsync(factory);
            Guid product = await arrange.CreateProductAsync(price: 10m, stock: 10);
            (Guid orderId, Guid orderItemId) = await arrange.CreateOrderAsync(userId, product, amount: 2);

            HttpResponseMessage updateOrder = await client.PutAsJsonAsync($"{OrderArrange.OrdersPath}/{orderId}", new { orderDate = DateTime.UtcNow }, cancellationToken);
            HttpResponseMessage addItem = await client.PostAsJsonAsync(OrderArrange.OrderItemsPath, new { orderId, productId = product, amount = 1 }, cancellationToken);
            HttpResponseMessage updateItem = await client.PutAsJsonAsync($"{OrderArrange.OrderItemsPath}/{orderItemId}", new { amount = 3 }, cancellationToken);
            HttpResponseMessage deleteItem = await client.DeleteAsync($"{OrderArrange.OrderItemsPath}/{orderItemId}", cancellationToken);
            HttpResponseMessage deleteOrder = await client.DeleteAsync($"{OrderArrange.OrdersPath}/{orderId}", cancellationToken);

            Assert.All([updateOrder, addItem, updateItem, deleteItem, deleteOrder], response => Assert.True(
                response.IsSuccessStatusCode, $"{response.RequestMessage?.Method} {response.RequestMessage?.RequestUri} -> {(int)response.StatusCode}"));
            Assert.Empty(payment.Requests);
            Assert.Empty(await arrange.GetOrdersOfAsync(userId));
            Assert.Equal(10, await arrange.GetStockAsync(product)); // silinen sipariş stoğu iade eder
        }
    }
}
