using ECommerce.Application.Services.Repositories;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

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
            return await uludagDbContext.Orders
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await uludagDbContext.Orders
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
