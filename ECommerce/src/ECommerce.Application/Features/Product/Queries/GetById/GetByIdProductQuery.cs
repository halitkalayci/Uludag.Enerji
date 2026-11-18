using ECommerce.Application.Features.Product.Mappers;
using ECommerce.Application.Features.Product.Rules;
using ECommerce.Application.Services.Repositories;
using ECommerce.Application.Features.Product.Constants;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.Product.Queries.GetById
{
    // Sorgu -> geriye değer döndüren istek. Ürün yoksa NotFoundException fırlar (null dönmez).
    public class GetByIdProductQuery : IRequest<ProductResponse>, ISecuredRequest
    {
        // Yetki: Product.Admin veya Product.Read claim'lerinden biri yeterli (global Admin her zaman geçer).
        // Açık (explicit) implementasyon: public property olmaz, JSON gövdesine / Swagger'a girmez.
        string[] ISecuredRequest.Roles => [ProductOperationClaims.Admin, ProductOperationClaims.Read];

        public Guid Id { get; set; }

        public class GetByIdProductQueryHandler : IRequestHandler<GetByIdProductQuery, ProductResponse>
        {
            // Dependency Injection
            private readonly IProductRepository productRepository;
            private readonly ProductBusinessRules productBusinessRules;
            public GetByIdProductQueryHandler(IProductRepository productRepository, ProductBusinessRules productBusinessRules)
            {
                this.productRepository = productRepository;
                this.productBusinessRules = productBusinessRules;
            }

            public async Task<ProductResponse> Handle(GetByIdProductQuery request, CancellationToken cancellationToken)
            {
                Domain.Entities.Product? product = await productRepository.GetByIdAsync(request.Id, cancellationToken);
                productBusinessRules.ProductShouldExistWhenSelected(product);

                // Mapping
                return ProductMapper.MapProductToResponse(product);
            }
        }
    }
}
