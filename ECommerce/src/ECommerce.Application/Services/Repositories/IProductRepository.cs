namespace ECommerce.Application.Services.Repositories
{
    // Product tablosu için önceden yazılmış komutların sözleşmesi.
    // Application yalnızca bu arayüzü bilir; nasıl uygulandığını (EF Core, Dapper, InMemory...) bilmez.
    // Implementasyon Persistence katmanındadır (Dependency Inversion).
    public interface IProductRepository
    {
        Task<List<Domain.Entities.Product>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Domain.Entities.Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task AddAsync(Domain.Entities.Product product, CancellationToken cancellationToken = default);
        Task UpdateAsync(Domain.Entities.Product product, CancellationToken cancellationToken = default);
        Task DeleteAsync(Domain.Entities.Product product, CancellationToken cancellationToken = default);
    }
}
