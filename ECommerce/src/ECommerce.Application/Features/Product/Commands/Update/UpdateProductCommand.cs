using ECommerce.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.Product.Commands.Update
{
    // Geriye bool döner: true -> güncellendi, false -> ürün bulunamadı.
    public class UpdateProductCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public int Stock { get; set; }

        public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, bool>
        {
            // Dependency Injection
            private readonly UludagDbContext uludagDbContext;
            public UpdateProductCommandHandler(UludagDbContext uludagDbContext)
            {
                this.uludagDbContext = uludagDbContext;
            }

            public async Task<bool> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
            {
                Domain.Entities.Product? product = await uludagDbContext.Products
                    .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

                if (product is null)
                    return false;

                // Mapping
                product.Name = request.Name;
                product.Price = new Domain.ValueObjects.Money(request.Amount, request.Currency);
                product.Stock = request.Stock;

                await uludagDbContext.SaveChangesAsync(cancellationToken);
                return true;
            }
        }
    }
}
