using ECommerce.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.Product.Commands.Delete
{
    // Geriye bool döner: true -> silindi, false -> ürün bulunamadı.
    public class DeleteProductCommand : IRequest<bool>
    {
        public Guid Id { get; set; }

        public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, bool>
        {
            // Dependency Injection
            private readonly UludagDbContext uludagDbContext;
            public DeleteProductCommandHandler(UludagDbContext uludagDbContext)
            {
                this.uludagDbContext = uludagDbContext;
            }

            public async Task<bool> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
            {
                Domain.Entities.Product? product = await uludagDbContext.Products
                    .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

                if (product is null)
                    return false;

                uludagDbContext.Products.Remove(product);
                await uludagDbContext.SaveChangesAsync(cancellationToken);
                return true;
            }
        }
    }
}
