namespace ECommerce.Application.Services.Repositories
{
    // uludag-gen ile üretildi (spec 0005); serbestçe düzenlenebilir.
    // Categories tablosu için önceden yazılmış komutların sözleşmesi.
    // Application yalnızca bu arayüzü bilir; implementasyon Persistence katmanındadır (Dependency Inversion).
    public interface ICategoryRepository
    {
        Task<List<Domain.Entities.Category>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Domain.Entities.Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        // Karşılaştırma kolonun collation'ıyla yapılır: büyük/küçük harf duyarsız (BR-12).
        Task<Domain.Entities.Category?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
        Task AddAsync(Domain.Entities.Category category, CancellationToken cancellationToken = default);
        Task UpdateAsync(Domain.Entities.Category category, CancellationToken cancellationToken = default);
        Task DeleteAsync(Domain.Entities.Category category, CancellationToken cancellationToken = default);
    }
}
