using ECommerce.Application.Exceptions;
using ECommerce.Application.Features.Category.Constants;
using System.Diagnostics.CodeAnalysis;

namespace ECommerce.Application.Features.Category.Rules
{
    // uludag-gen ile üretildi (spec 0005); serbestçe düzenlenebilir.
    // Category ile ilgili tüm iş kuralları burada yazılır; handler'lar if-else yapmaz, bu metotları çağırır (F-7).
    // Kural ihlal edilirse metot istisna fırlatır, edilmezse sessizce döner.
    // Otomatik DI kaydı: BaseBusinessRules'tan türediği için AddSubClassesOfType ile Scoped eklenir.
    public class CategoryBusinessRules : BaseBusinessRules
    {
        // [NotNull]: Metot hata fırlatmadan dönerse derleyici kaydın artık null olmadığını bilir.
        public void CategoryShouldExistWhenSelected([NotNull] Domain.Entities.Category? category)
        {
            if (category is null)
                throw new NotFoundException(CategoryConstants.CategoryNotFound);
        }
    }
}
