using ECommerce.Application.Exceptions;
using ECommerce.Application.Features.Category.Constants;
using ECommerce.Application.Services.ProductService;
using ECommerce.Application.Services.Repositories;
using System.Diagnostics.CodeAnalysis;

namespace ECommerce.Application.Features.Category.Rules
{
    // uludag-gen ile üretildi (spec 0005); spec 0009 kurallarıyla düzenlendi.
    // Category ile ilgili tüm iş kuralları burada yazılır; handler'lar if-else yapmaz, bu metotları çağırır (F-7).
    // Kural ihlal edilirse metot istisna fırlatır, edilmezse sessizce döner.
    // Otomatik DI kaydı: BaseBusinessRules'tan türediği için AddSubClassesOfType ile Scoped eklenir.
    public class CategoryBusinessRules : BaseBusinessRules
    {
        private readonly ICategoryRepository categoryRepository;
        // Ürün tablosuna kendi repository'si ile değil, Product feature'ının servisiyle bakılır (F-6).
        private readonly IProductService productService;
        public CategoryBusinessRules(ICategoryRepository categoryRepository, IProductService productService)
        {
            this.categoryRepository = categoryRepository;
            this.productService = productService;
        }

        // [NotNull]: Metot hata fırlatmadan dönerse derleyici kaydın artık null olmadığını bilir.
        public void CategoryShouldExistWhenSelected([NotNull] Domain.Entities.Category? category)
        {
            if (category is null)
                throw new NotFoundException(CategoryConstants.CategoryNotFound);
        }

        // BR-12 (girdi kısmı): ad zorunlu, kırpıldıktan sonra boş olamaz ve en fazla 100 karakterdir.
        public void CategoryNameShouldBeValid([NotNull] string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new BusinessException(CategoryConstants.CategoryNameCannotBeEmpty);

            if (name.Trim().Length > Domain.Entities.Category.NameMaxLength)
                throw new BusinessException(CategoryConstants.CategoryNameTooLong);
        }

        // BR-12: ad benzersizdir, büyük/küçük harf farkı gözetilmez (kolonun collation'ı, CategoryConfiguration).
        // Ad değiştirilirken kategorinin kendisi çakışma sayılmaz (exceptCategoryId).
        public async Task CategoryNameCannotExist(string name, Guid? exceptCategoryId = null, CancellationToken cancellationToken = default)
        {
            Domain.Entities.Category? categoryWithSameName = await categoryRepository.GetByNameAsync(name.Trim(), cancellationToken);
            if (categoryWithSameName is not null && categoryWithSameName.Id != exceptCategoryId)
                throw new BusinessException(CategoryConstants.CategoryNameAlreadyExists);
        }

        // BR-14: ürünü olan kategori silinemez.
        public async Task CategoryShouldHaveNoProducts(Guid categoryId, CancellationToken cancellationToken = default)
        {
            if (await productService.AnyInCategoryAsync(categoryId, cancellationToken))
                throw new BusinessException(CategoryConstants.CategoryHasProducts);
        }
    }
}
