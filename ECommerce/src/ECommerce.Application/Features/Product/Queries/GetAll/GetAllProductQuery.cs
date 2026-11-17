using ECommerce.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.Product.Queries.GetAll
{
    // Gövdesi boş sorgu: tüm ürünleri ister.
    public class GetAllProductQuery : IRequest<List<ProductResponse>>
    {
        public class GetAllProductQueryHandler : IRequestHandler<GetAllProductQuery, List<ProductResponse>>
        {
            // Dependency Injection
            private readonly UludagDbContext uludagDbContext;
            public GetAllProductQueryHandler(UludagDbContext uludagDbContext)
            {
                this.uludagDbContext = uludagDbContext;
            }

            public async Task<List<ProductResponse>> Handle(GetAllProductQuery request, CancellationToken cancellationToken)
            {
                // Mapping (projeksiyon -> SQL'de sadece gereken kolonlar seçilir)
                return await uludagDbContext.Products
                    .AsNoTracking()
                    .Select(p => new ProductResponse
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Amount = p.Price.Amount,
                        Currency = p.Price.Currency,
                        Stock = p.Stock
                    })
                    .ToListAsync(cancellationToken);
            }
        }
    }
}
