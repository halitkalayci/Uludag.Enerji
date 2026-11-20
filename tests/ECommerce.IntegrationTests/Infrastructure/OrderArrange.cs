using System.Globalization;
using System.Net.Http.Headers;
using ECommerce.Application.Services.PaymentService;
using ECommerce.Domain.Entities;
using ECommerce.Domain.ValueObjects;
using ECommerce.Persistence.Contexts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
// Test klasörü ECommerce.IntegrationTests.Order ad alanı entity adını gölgeler.
using OrderEntity = ECommerce.Domain.Entities.Order;

namespace ECommerce.IntegrationTests.Infrastructure
{
    // Sipariş/ödeme testlerinin HAZIRLIK ve GÖZLEM adımları (spec 0008). Testin konusu olan istek (sipariş verme vb.)
    // her zaman testin kendisinde, HTTP üzerinden yapılır; burada yalnızca ön koşullar kurulur ve sonuç veritabanından okunur.
    public sealed class OrderArrange(ApiFixture fixture)
    {
        public const string OrdersPath = "/api/orders";
        public const string OrderItemsPath = "/api/orderitems";
        public const string Currency = "TRY";

        private const string DeclineRateSetting = "FakePayment:DeclineRate";

        // Sipariş ve sipariş kalemi işlemlerinin tamamına yetki (OperationClaimSeeds'teki adlarla).
        private static readonly string[] ClaimNames = ["Order.Admin", "OrderItem.Admin"];

        private readonly AuthArrange auth = new(fixture);

        // Paylaşılan container'ları kullanan, ret oranı verilmiş ayrı bir uygulama örneği (gerçek FakePaymentService).
        public WebApplicationFactory<Program> FactoryWithDeclineRate(double declineRate) =>
            fixture.Factory.WithWebHostBuilder(builder =>
                builder.UseSetting(DeclineRateSetting, declineRate.ToString(CultureInfo.InvariantCulture)));

        // Ödeme sağlayıcısı stub'la değiştirilmiş uygulama örneği.
        public WebApplicationFactory<Program> FactoryWithPayment(IPaymentService paymentService) =>
            fixture.Factory.WithWebHostBuilder(builder =>
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IPaymentService>();
                    services.AddSingleton(paymentService);
                }));

        // Sipariş ve kalem yetkileri olan yeni bir kullanıcı; verilen uygulama örneğine Bearer token'ıyla bağlı istemci.
        // Token oturumu Redis'te olduğu için aynı container'ları kullanan her örnekte geçerlidir.
        public async Task<(Guid UserId, HttpClient Client)> CreateCustomerAsync(WebApplicationFactory<Program> factory)
        {
            Guid userId = await auth.CreateUserAsync(AuthArrange.NewEmail());
            await GrantClaimsAsync(userId);

            using HttpClient authClient = auth.CreateClient();
            (_, TokenResult tokens) = await auth.EnableMfaAsync(authClient, userId);

            HttpClient client = factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
            return (userId, client);
        }

        public async Task<Guid> CreateProductAsync(decimal price, int stock, string currency = Currency)
        {
            Product product = new()
            {
                Id = Guid.NewGuid(),
                Name = $"product-{Guid.NewGuid():N}",
                Price = new Money(price, currency),
                Stock = stock
            };

            await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
            UludagDbContext dbContext = scope.ServiceProvider.GetRequiredService<UludagDbContext>();
            dbContext.Products.Add(product);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
            return product.Id;
        }

        // Kalemleriyle var olan bir sipariş (AC-7'nin ön koşulu); stok, kalem adedi kadar düşülmüş kabul edilir.
        public async Task<(Guid OrderId, Guid OrderItemId)> CreateOrderAsync(Guid userId, Guid productId, int amount)
        {
            await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
            UludagDbContext dbContext = scope.ServiceProvider.GetRequiredService<UludagDbContext>();

            Product product = await dbContext.Products.SingleAsync(p => p.Id == productId, TestContext.Current.CancellationToken);
            Money itemTotal = new(product.Price.Amount * amount, product.Price.Currency);
            OrderItem item = new() { Id = Guid.NewGuid(), ProductId = productId, Amount = amount, TotalPrice = itemTotal };
            OrderEntity order = new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                OrderDate = DateTime.UtcNow,
                TotalPrice = new Money(itemTotal.Amount, itemTotal.Currency),
                Items = { item }
            };
            product.Stock -= amount;

            dbContext.Orders.Add(order);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
            return (order.Id, item.Id);
        }

        public async Task<int> GetStockAsync(Guid productId)
        {
            await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
            UludagDbContext dbContext = scope.ServiceProvider.GetRequiredService<UludagDbContext>();
            return await dbContext.Products.AsNoTracking()
                .Where(p => p.Id == productId)
                .Select(p => p.Stock)
                .SingleAsync(TestContext.Current.CancellationToken);
        }

        public async Task<List<OrderEntity>> GetOrdersOfAsync(Guid userId)
        {
            await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
            UludagDbContext dbContext = scope.ServiceProvider.GetRequiredService<UludagDbContext>();
            return await dbContext.Orders.AsNoTracking()
                .Include(o => o.Items)
                .Where(o => o.UserId == userId)
                .ToListAsync(TestContext.Current.CancellationToken);
        }

        private async Task GrantClaimsAsync(Guid userId)
        {
            await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
            UludagDbContext dbContext = scope.ServiceProvider.GetRequiredService<UludagDbContext>();

            List<Guid> claimIds = await dbContext.OperationClaims
                .Where(c => ClaimNames.Contains(c.Name))
                .Select(c => c.Id)
                .ToListAsync(TestContext.Current.CancellationToken);
            Assert.Equal(ClaimNames.Length, claimIds.Count);

            foreach (Guid claimId in claimIds)
                dbContext.UserOperationClaims.Add(new UserOperationClaim { Id = Guid.NewGuid(), UserId = userId, OperationClaimId = claimId });

            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
    }
}
