using ECommerce.Application.Features.Category.Rules;
using ECommerce.Application.Services.Repositories;

namespace ECommerce.Application.Services.CategoryService
{
    // ICategoryService'in implementasyonu. Yalnızca kendi repository'sini ve kendi feature'ının kurallarını kullanır.
    public class CategoryManager : ICategoryService
    {
        private readonly ICategoryRepository categoryRepository;
        private readonly CategoryBusinessRules categoryBusinessRules;
        public CategoryManager(ICategoryRepository categoryRepository, CategoryBusinessRules categoryBusinessRules)
        {
            this.categoryRepository = categoryRepository;
            this.categoryBusinessRules = categoryBusinessRules;
        }

        public async Task CategoryShouldExistWhenGivenAsync(Guid? categoryId, CancellationToken cancellationToken = default)
        {
            // Kategorisiz ürün geçerlidir (BR-13: sıfır ya da bir kategori). Bu tolerans handler'da değil, serviste.
            if (categoryId is null)
                return;

            Domain.Entities.Category? category = await categoryRepository.GetByIdAsync(categoryId.Value, cancellationToken);
            categoryBusinessRules.CategoryShouldExistWhenSelected(category);
        }
    }
}
