namespace ECommerce.Application.Services.CategoryService
{
    // Başka feature'ların (Product) Category'ye tek kapısı (F-6): ICategoryRepository'yi değil bu servisi kullanırlar.
    // Kuralların kendisi CategoryBusinessRules'tadır; servis onları çağırır.
    public interface ICategoryService
    {
        // Kategori verilmediyse (null) hiçbir şey yapmaz; verildiyse ve yoksa NotFoundException fırlatır (404).
        Task CategoryShouldExistWhenGivenAsync(Guid? categoryId, CancellationToken cancellationToken = default);
    }
}
