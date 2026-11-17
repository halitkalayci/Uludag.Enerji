using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.Product.Queries.GetById
{
    // Sorgu -> geriye değer döndüren istek. Ürün yoksa null döner.
    public class GetByIdProductQuery : IRequest<ProductResponse?>
    {
        public Guid Id { get; set; }

        public class GetByIdProductQueryHandler : IRequestHandler<GetByIdProductQuery, ProductResponse?>
        {
            // Dependency Injection
            private readonly IProductRepository productRepository;
            public GetByIdProductQueryHandler(IProductRepository productRepository)
            {
                this.productRepository = productRepository;
            }

            public async Task<ProductResponse?> Handle(GetByIdProductQuery request, CancellationToken cancellationToken)
            {
                Domain.Entities.Product? product = await productRepository.GetByIdAsync(request.Id, cancellationToken);

                if (product is null)
                    return null;

                // Mapping
                return new ProductResponse
                {
                    Id = product.Id,
                    Name = product.Name,
                    Amount = product.Price.Amount,
                    Currency = product.Price.Currency,
                    Stock = product.Stock
                };
            }
        }
    }
}
