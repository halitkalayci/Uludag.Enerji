using ECommerce.Application.Features.Product.Mappers;
using ECommerce.Application.Services.Repositories;
using ECommerce.Application.Features.Product.Constants;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.Product.Queries.GetAll
{
    // Gövdesi boş sorgu: tüm ürünleri ister.
    public class GetAllProductQuery : IRequest<List<ProductResponse>>
    {
        // Yetki: Product.Admin veya Product.Read claim'lerinden biri yeterli (global Admin her zaman geçer).
        // Açık (explicit) implementasyon: public property olmaz, JSON gövdesine / Swagger'a girmez.

        public class GetAllProductQueryHandler : IRequestHandler<GetAllProductQuery, List<ProductResponse>>
        {
            // Dependency Injection
            private readonly IProductRepository productRepository;
            public GetAllProductQueryHandler(IProductRepository productRepository)
            {
                this.productRepository = productRepository;
            }

            public async Task<List<ProductResponse>> Handle(GetAllProductQuery request, CancellationToken cancellationToken)
            {
                List<Domain.Entities.Product> products = await productRepository.GetAllAsync(cancellationToken);

                // Mapping
                return ProductMapper.MapProductsToResponses(products);
            }
        }
    }
}
