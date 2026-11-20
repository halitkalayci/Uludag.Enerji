using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ECommerce.Application.Services.PaymentService;
using ECommerce.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ECommerce.IntegrationTests.Order
{
    // Sipariş oluştururken ödeme (spec 0008, AC-1…AC-4). Beklentiler spec'ten: ret → sipariş yok, stok aynı;
    // tutarı sunucu belirler; geçersiz istek ödemeye hiç gitmez.
    [Collection(IntegrationCollection.Name)]
    [Trait("Category", "Integration")]
    public class CreateOrderPaymentTests(ApiFixture fixture)
    {
        private const string PaymentDeclined = "PaymentDeclined";

        private readonly OrderArrange arrange = new(fixture);

        // AC-1
        [Fact]
        public async Task CreateOrder_PaymentApproved_CreatesOrderAndDecreasesStock()
        {
            await using WebApplicationFactory<Program> factory = arrange.FactoryWithDeclineRate(0);
            (Guid userId, HttpClient client) = await arrange.CreateCustomerAsync(factory);
            Guid pen = await arrange.CreateProductAsync(price: 10m, stock: 5);
            Guid book = await arrange.CreateProductAsync(price: 7.5m, stock: 3);

            HttpResponseMessage response = await PlaceOrderAsync(client, (pen, 2), (book, 1));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Domain.Entities.Order order = Assert.Single(await arrange.GetOrdersOfAsync(userId));
            Assert.Equal(2, order.Items.Count);
            Assert.Equal(27.5m, order.TotalPrice.Amount);
            Assert.Equal(3, await arrange.GetStockAsync(pen));
            Assert.Equal(2, await arrange.GetStockAsync(book));
        }

        // AC-2
        [Fact]
        public async Task CreateOrder_PaymentDeclined_Returns400AndChangesNothing()
        {
            await using WebApplicationFactory<Program> factory = arrange.FactoryWithDeclineRate(1);
            (Guid userId, HttpClient client) = await arrange.CreateCustomerAsync(factory);
            Guid pen = await arrange.CreateProductAsync(price: 10m, stock: 5);
            Guid book = await arrange.CreateProductAsync(price: 7.5m, stock: 3);

            HttpResponseMessage response = await PlaceOrderAsync(client, (pen, 2), (book, 1));

            JsonElement problem = await ProblemAssert.IsProblemAsync(response, HttpStatusCode.BadRequest);
            Assert.Equal(PaymentDeclined, problem.GetProperty("detail").GetString());
            Assert.Empty(await arrange.GetOrdersOfAsync(userId));
            Assert.Equal(5, await arrange.GetStockAsync(pen));
            Assert.Equal(3, await arrange.GetStockAsync(book));
        }

        // AC-3: aynı üründen iki kalem + başka bir ürün — tutar sunucudaki fiyatlardan, istemci tutar göndermez.
        [Fact]
        public async Task CreateOrder_MultipleItems_ChargesServerComputedTotal()
        {
            RecordingPaymentService payment = new(approve: true);
            await using WebApplicationFactory<Program> factory = arrange.FactoryWithPayment(payment);
            (Guid userId, HttpClient client) = await arrange.CreateCustomerAsync(factory);
            Guid pen = await arrange.CreateProductAsync(price: 10m, stock: 5);
            Guid book = await arrange.CreateProductAsync(price: 7.5m, stock: 3);

            HttpResponseMessage response = await PlaceOrderAsync(client, (pen, 2), (book, 1), (pen, 1));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            PaymentRequest charged = Assert.Single(payment.Requests);
            Assert.Equal(37.5m, charged.Amount.Amount);
            Assert.Equal(OrderArrange.Currency, charged.Amount.Currency);
            Assert.Equal(userId, charged.UserId);

            Domain.Entities.Order order = Assert.Single(await arrange.GetOrdersOfAsync(userId));
            Assert.Equal(order.TotalPrice.Amount, charged.Amount.Amount);
            Assert.Equal(order.TotalPrice.Currency, charged.Amount.Currency);
        }

        public static TheoryData<string> InvalidRequests => new()
        {
            "no-items",
            "zero-amount",
            "missing-product",
            "insufficient-stock",
            "currency-mismatch",
        };

        // AC-4: ret oranı 1 olsa da geçersiz istek kendi hatasını alır, ödeme hiç denenmez.
        [Theory]
        [MemberData(nameof(InvalidRequests))]
        public async Task CreateOrder_InvalidRequest_ReturnsOwnErrorWithoutPayment(string invalidCase)
        {
            RecordingPaymentService payment = new(approve: false);
            await using WebApplicationFactory<Program> factory = arrange.FactoryWithPayment(payment);
            (Guid userId, HttpClient client) = await arrange.CreateCustomerAsync(factory);
            Guid product = await arrange.CreateProductAsync(price: 10m, stock: 2);
            Guid usdProduct = await arrange.CreateProductAsync(price: 10m, stock: 2, currency: "USD");

            (HttpResponseMessage response, HttpStatusCode expectedStatus, string expectedDetail) = invalidCase switch
            {
                "no-items" => (await PlaceOrderAsync(client), HttpStatusCode.BadRequest, "OrderMustContainAtLeastOneItem"),
                "zero-amount" => (await PlaceOrderAsync(client, (product, 0)), HttpStatusCode.BadRequest, "Amount must be greater than zero."),
                "missing-product" => (await PlaceOrderAsync(client, (Guid.NewGuid(), 1)), HttpStatusCode.NotFound, "Product not found."),
                "insufficient-stock" => (await PlaceOrderAsync(client, (product, 3)), HttpStatusCode.BadRequest, $"Insufficient stock for product {product}."),
                "currency-mismatch" => (await PlaceOrderAsync(client, (usdProduct, 1)), HttpStatusCode.BadRequest, "Product currency does not match the order currency."),
                _ => throw new ArgumentOutOfRangeException(nameof(invalidCase), invalidCase, null)
            };

            JsonElement problem = await ProblemAssert.IsProblemAsync(response, expectedStatus);
            Assert.Equal(expectedDetail, problem.GetProperty("detail").GetString());
            Assert.Empty(payment.Requests);
            Assert.Empty(await arrange.GetOrdersOfAsync(userId));
            Assert.Equal(2, await arrange.GetStockAsync(product));
        }

        private static Task<HttpResponseMessage> PlaceOrderAsync(HttpClient client, params (Guid ProductId, int Amount)[] items) =>
            client.PostAsJsonAsync(
                OrderArrange.OrdersPath,
                new { currency = OrderArrange.Currency, items = items.Select(i => new { productId = i.ProductId, amount = i.Amount }) },
                TestContext.Current.CancellationToken);
    }
}
