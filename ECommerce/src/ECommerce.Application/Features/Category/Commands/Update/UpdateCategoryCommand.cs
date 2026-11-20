using ECommerce.Application.Features.Category.Constants;
using ECommerce.Application.Features.Category.Mappers;
using ECommerce.Application.Features.Category.Rules;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.Category.Commands.Update
{
    // uludag-gen ile üretildi (spec 0005); serbestçe düzenlenebilir.
    // Tam değiştirme (PUT): tüm alanlar gönderilir. Kayıt bulunamazsa NotFoundException fırlar (404).
    public class UpdateCategoryCommand : IRequest, ISecuredRequest
    {
        // Yetki: Category.Admin, Category.Write veya Category.Update claim'lerinden biri yeterli (global Admin her zaman geçer).
        // Açık (explicit) implementasyon: public property olmaz, JSON gövdesine / Swagger'a girmez.
        string[] ISecuredRequest.Roles => [CategoryOperationClaims.Admin, CategoryOperationClaims.Write, CategoryOperationClaims.Update];

        public Guid Id { get; set; }
        public string Name { get; set; } = null!;

        public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand>
        {
            private readonly ICategoryRepository categoryRepository;
            private readonly CategoryBusinessRules categoryBusinessRules;
            public UpdateCategoryCommandHandler(ICategoryRepository categoryRepository, CategoryBusinessRules categoryBusinessRules)
            {
                this.categoryRepository = categoryRepository;
                this.categoryBusinessRules = categoryBusinessRules;
            }

            public async Task Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
            {
                Domain.Entities.Category? category = await categoryRepository.GetByIdAsync(request.Id, cancellationToken);
                categoryBusinessRules.CategoryShouldExistWhenSelected(category);
                categoryBusinessRules.CategoryNameShouldBeValid(request.Name);
                await categoryBusinessRules.CategoryNameCannotExist(request.Name, category.Id, cancellationToken);

                CategoryMapper.MapUpdateCommandToCategory(request, category);

                await categoryRepository.UpdateAsync(category, cancellationToken);
            }
        }
    }
}
