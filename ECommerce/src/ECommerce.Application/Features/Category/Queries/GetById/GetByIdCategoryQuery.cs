using ECommerce.Application.Features.Category.Constants;
using ECommerce.Application.Features.Category.Mappers;
using ECommerce.Application.Features.Category.Rules;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.Category.Queries.GetById
{
    // uludag-gen ile üretildi (spec 0005); serbestçe düzenlenebilir.
    // Kayıt yoksa NotFoundException fırlar (404); null dönmez.
    public class GetByIdCategoryQuery : IRequest<CategoryResponse>, ISecuredRequest
    {
        // Yetki: Category.Admin veya Category.Read claim'lerinden biri yeterli (global Admin her zaman geçer).
        // Açık (explicit) implementasyon: public property olmaz, JSON gövdesine / Swagger'a girmez.
        string[] ISecuredRequest.Roles => [CategoryOperationClaims.Admin, CategoryOperationClaims.Read];

        public Guid Id { get; set; }

        public class GetByIdCategoryQueryHandler : IRequestHandler<GetByIdCategoryQuery, CategoryResponse>
        {
            private readonly ICategoryRepository categoryRepository;
            private readonly CategoryBusinessRules categoryBusinessRules;
            public GetByIdCategoryQueryHandler(ICategoryRepository categoryRepository, CategoryBusinessRules categoryBusinessRules)
            {
                this.categoryRepository = categoryRepository;
                this.categoryBusinessRules = categoryBusinessRules;
            }

            public async Task<CategoryResponse> Handle(GetByIdCategoryQuery request, CancellationToken cancellationToken)
            {
                Domain.Entities.Category? category = await categoryRepository.GetByIdAsync(request.Id, cancellationToken);
                categoryBusinessRules.CategoryShouldExistWhenSelected(category);

                return CategoryMapper.MapCategoryToResponse(category);
            }
        }
    }
}
