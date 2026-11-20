using ECommerce.Application.Features.Product.Mappers;
using ECommerce.Application.Features.Product.Rules;
using ECommerce.Application.Services.Repositories;
using ECommerce.Application.Features.Product.Constants;
using ECommerce.Application.Services.CategoryService;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.Product.Commands.Update
{
    // Ürün bulunamazsa NotFoundException fırlar.
    public class UpdateProductCommand : IRequest, ISecuredRequest
    {
        // Yetki: Product.Admin, Product.Write veya Product.Update claim'lerinden biri yeterli (global Admin her zaman geçer).
        // Açık (explicit) implementasyon: public property olmaz, JSON gövdesine / Swagger'a girmez.
        string[] ISecuredRequest.Roles => [ProductOperationClaims.Admin, ProductOperationClaims.Write, ProductOperationClaims.Update];

        public Guid Id { get; set; }
        public string Name { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public int Stock { get; set; }
        // Ürünün kategorisi (spec 0009); null ya da gönderilmezse ürün kategorisizdir.
        public Guid? CategoryId { get; set; }

        public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand>
        {
            // Dependency Injection
            private readonly IProductRepository productRepository;
            private readonly ProductBusinessRules productBusinessRules;
            // Kategori başka feature'ın entity'si: repository'si değil servisi kullanılır (F-6).
            private readonly ICategoryService categoryService;
            public UpdateProductCommandHandler(IProductRepository productRepository, ProductBusinessRules productBusinessRules, ICategoryService categoryService)
            {
                this.productRepository = productRepository;
                this.productBusinessRules = productBusinessRules;
                this.categoryService = categoryService;
            }

            public async Task Handle(UpdateProductCommand request, CancellationToken cancellationToken)
            {
                Domain.Entities.Product? product = await productRepository.GetByIdAsync(request.Id, cancellationToken);
                productBusinessRules.ProductShouldExistWhenSelected(product);
                await categoryService.CategoryShouldExistWhenGivenAsync(request.CategoryId, cancellationToken);

                // Mapping
                ProductMapper.MapUpdateCommandToProduct(request, product);

                await productRepository.UpdateAsync(product, cancellationToken);
            }
        }
    }
}
