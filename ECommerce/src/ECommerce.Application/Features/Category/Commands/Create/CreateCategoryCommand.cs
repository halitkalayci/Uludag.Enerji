using ECommerce.Application.Features.Category.Constants;
using ECommerce.Application.Features.Category.Mappers;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.Category.Commands.Create
{
    // uludag-gen ile üretildi (spec 0005); serbestçe düzenlenebilir.
    // Entity'ye özgü iş kuralları (ör. ad benzersizliği) CategoryBusinessRules'a eklenir ve handler'dan çağrılır.
    public class CreateCategoryCommand : IRequest, ISecuredRequest
    {
        // Yetki: Category.Admin, Category.Write veya Category.Create claim'lerinden biri yeterli (global Admin her zaman geçer).
        // Açık (explicit) implementasyon: public property olmaz, JSON gövdesine / Swagger'a girmez.
        string[] ISecuredRequest.Roles => [CategoryOperationClaims.Admin, CategoryOperationClaims.Write, CategoryOperationClaims.Create];

        public string Name { get; set; } = null!;

        public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand>
        {
            private readonly ICategoryRepository categoryRepository;
            public CreateCategoryCommandHandler(ICategoryRepository categoryRepository)
            {
                this.categoryRepository = categoryRepository;
            }

            public async Task Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
            {
                Domain.Entities.Category category = CategoryMapper.MapCreateCommandToCategory(request);

                await categoryRepository.AddAsync(category, cancellationToken);
            }
        }
    }
}
