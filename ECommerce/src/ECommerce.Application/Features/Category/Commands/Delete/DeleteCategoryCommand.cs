using ECommerce.Application.Features.Category.Constants;
using ECommerce.Application.Features.Category.Rules;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.Category.Commands.Delete
{
    // uludag-gen ile üretildi (spec 0005); serbestçe düzenlenebilir.
    // Kayıt bulunamazsa NotFoundException fırlar (404); ürünü olan kategori silinemez (BR-14, 400).
    public class DeleteCategoryCommand : IRequest, ISecuredRequest
    {
        // Yetki: Category.Admin, Category.Write veya Category.Delete claim'lerinden biri yeterli (global Admin her zaman geçer).
        // Açık (explicit) implementasyon: public property olmaz, JSON gövdesine / Swagger'a girmez.
        string[] ISecuredRequest.Roles => [CategoryOperationClaims.Admin, CategoryOperationClaims.Write, CategoryOperationClaims.Delete];

        public Guid Id { get; set; }

        public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand>
        {
            private readonly ICategoryRepository categoryRepository;
            private readonly CategoryBusinessRules categoryBusinessRules;
            public DeleteCategoryCommandHandler(ICategoryRepository categoryRepository, CategoryBusinessRules categoryBusinessRules)
            {
                this.categoryRepository = categoryRepository;
                this.categoryBusinessRules = categoryBusinessRules;
            }

            public async Task Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
            {
                Domain.Entities.Category? category = await categoryRepository.GetByIdAsync(request.Id, cancellationToken);
                categoryBusinessRules.CategoryShouldExistWhenSelected(category);
                await categoryBusinessRules.CategoryShouldHaveNoProducts(category.Id, cancellationToken);

                await categoryRepository.DeleteAsync(category, cancellationToken);
            }
        }
    }
}
