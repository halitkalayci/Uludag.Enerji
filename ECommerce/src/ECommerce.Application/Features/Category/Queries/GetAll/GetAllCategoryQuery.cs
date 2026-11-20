using ECommerce.Application.Features.Category.Constants;
using ECommerce.Application.Features.Category.Mappers;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.Category.Queries.GetAll
{
    // uludag-gen ile üretildi (spec 0005); serbestçe düzenlenebilir.
    // Gövdesi boş sorgu: tüm kayıtları ister.
    public class GetAllCategoryQuery : IRequest<List<CategoryResponse>>, ISecuredRequest
    {
        // Yetki: Category.Admin veya Category.Read claim'lerinden biri yeterli (global Admin her zaman geçer).
        // Açık (explicit) implementasyon: public property olmaz, JSON gövdesine / Swagger'a girmez.
        string[] ISecuredRequest.Roles => [CategoryOperationClaims.Admin, CategoryOperationClaims.Read];

        public class GetAllCategoryQueryHandler : IRequestHandler<GetAllCategoryQuery, List<CategoryResponse>>
        {
            private readonly ICategoryRepository categoryRepository;
            public GetAllCategoryQueryHandler(ICategoryRepository categoryRepository)
            {
                this.categoryRepository = categoryRepository;
            }

            public async Task<List<CategoryResponse>> Handle(GetAllCategoryQuery request, CancellationToken cancellationToken)
            {
                List<Domain.Entities.Category> categories = await categoryRepository.GetAllAsync(cancellationToken);

                return CategoryMapper.MapCategoriesToResponses(categories);
            }
        }
    }
}
