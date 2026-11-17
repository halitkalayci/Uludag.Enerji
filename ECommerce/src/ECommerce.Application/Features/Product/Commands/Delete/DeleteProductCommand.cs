using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.Product.Commands.Delete
{
    // Geriye bool döner: true -> silindi, false -> ürün bulunamadı.
    public class DeleteProductCommand : IRequest<bool>
    {
        public Guid Id { get; set; }

        public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, bool>
        {
            // Dependency Injection

            /// Hiç bir application katmanı veritabanına (Context) direkt ulaşım sağlayamaz.
            /// Repository Pattern -> DB'yi repolara böl, ProductRepo, OrderRepo, CategoryRepo -> Spesifik bir tabloyla çalışan
            /// önceden yazılmış komutlar, bu komutları çağırarak db işlemleri yapılsın.
            /// Developer'a direkt veritabanı erişimi sağladın.
            /// Developer belirler X
            /// Developerı doğruyu kullanmaya zorunlu tutmam. ✔️
            private readonly IProductRepository productRepository;
            public DeleteProductCommandHandler(IProductRepository productRepository)
            {
                this.productRepository = productRepository;
            }

            public async Task<bool> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
            {
                Domain.Entities.Product? product = await productRepository.GetByIdAsync(request.Id, cancellationToken);

                if (product is null)
                    return false;

                await productRepository.DeleteAsync(product, cancellationToken);
                return true;
            }
        }
    }
}
