using ECommerce.Application.Services.Repositories;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories
{
    // IOrderItemRepository'nin EF Core ile yazılmış hali.
    // Context'e yalnızca bu katman dokunur.
    public class OrderItemRepository : IOrderItemRepository
    {
        private readonly UludagDbContext uludagDbContext;
        public OrderItemRepository(UludagDbContext uludagDbContext)
        {
            this.uludagDbContext = uludagDbContext;
        }

        public async Task<List<OrderItem>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await uludagDbContext.OrderItems
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<OrderItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await uludagDbContext.OrderItems
                .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        }

        public async Task AddAsync(OrderItem orderItem, CancellationToken cancellationToken = default)
        {
            await uludagDbContext.OrderItems.AddAsync(orderItem, cancellationToken);
            await uludagDbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(OrderItem orderItem, CancellationToken cancellationToken = default)
        {
            uludagDbContext.OrderItems.Update(orderItem);
            await uludagDbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(OrderItem orderItem, CancellationToken cancellationToken = default)
        {
            uludagDbContext.OrderItems.Remove(orderItem);
            await uludagDbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
