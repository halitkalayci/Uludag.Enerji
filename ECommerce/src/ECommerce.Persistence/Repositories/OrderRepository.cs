using ECommerce.Application.Services.Repositories;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Order = ECommerce.Domain.Entities.Order;

namespace ECommerce.Persistence.Repositories
{
    // IOrderRepository'nin EF Core ile yazılmış hali.
    // Context'e yalnızca bu katman dokunur.
    public class OrderRepository : IOrderRepository
    {
        private readonly UludagDbContext uludagDbContext;
        public OrderRepository(UludagDbContext uludagDbContext)
        {
            this.uludagDbContext = uludagDbContext;
        }

        public async Task<List<Order>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            //var result = uludagDbContext.Orders.FromSqlRaw($"Select * from Orders where true; DROP TABLE orders;{input}");
            // CMD => 
            // SQL,
            // XSS -> Metin birleştirme yasak, kullanılabiliyorsa parametrik birleştirme, kullanılamıyorsa SANITIZE işlemi.
            return await uludagDbContext.Orders
                .AsNoTracking()
                .ToListAsync(cancellationToken);

        }

        public async Task<List<Order>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            // bu kod SQL Injection'a açıktır, EF Core'un Where() metodu kullanılmalıydı.

            /// Injection -> Metini direkt sql'e (cmd, xss) asla gönderme. 
            /// Never Trust User Input.
            return await uludagDbContext.Orders
                .AsNoTracking()
                .Where(o => o.UserId == userId)
                .ToListAsync(cancellationToken); // EF Core -> SQL Transform -> SQL Injection'a kapalı.
        }

        public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await uludagDbContext.Orders
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        }

        public async Task<Order?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await uludagDbContext.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        }

        // order.Items doluysa kalemler de aynı SaveChanges ile (tek seferde) eklenir.
        public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
        {
            await uludagDbContext.Orders.AddAsync(order, cancellationToken);
            await uludagDbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
        {
            uludagDbContext.Orders.Update(order);
            await uludagDbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Order order, CancellationToken cancellationToken = default)
        {
            // Cascade, yalnızca context'in takip ettiği kalemlere uygulanır (InMemory provider'da FK yok).
            // OrderItems tablosuna doğrudan gitmek yerine siparişin kendi navigation'ını yüklüyoruz;
            // böylece repository yalnızca Order aggregate'i üzerinden çalışır.
            await uludagDbContext.Entry(order)
                .Collection(o => o.Items)
                .LoadAsync(cancellationToken);

            uludagDbContext.Orders.Remove(order);
            await uludagDbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
