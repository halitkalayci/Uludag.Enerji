using ECommerce.Application.Features.Product.Rules;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.Product.Commands.Delete
{
    // Ürün bulunamazsa NotFoundException fırlar.
    public class DeleteProductCommand : IRequest
    {
        public Guid Id { get; set; }

        public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand>
        {
            // Dependency Injection

            /// Hiç bir application katmanı veritabanına (Context) direkt ulaşım sağlayamaz.
            /// Repository Pattern -> DB'yi repolara böl, ProductRepo, OrderRepo, CategoryRepo -> Spesifik bir tabloyla çalışan
            /// önceden yazılmış komutlar, bu komutları çağırarak db işlemleri yapılsın.
            /// Developer'a direkt veritabanı erişimi sağladın.
            /// Developer belirler X
            /// Developerı doğruyu kullanmaya zorunlu tutmam. ✔️
            private readonly IProductRepository productRepository;
            private readonly ProductBusinessRules productBusinessRules;
            public DeleteProductCommandHandler(IProductRepository productRepository, ProductBusinessRules productBusinessRules)
            {
                this.productRepository = productRepository;
                this.productBusinessRules = productBusinessRules;
            }

            public async Task Handle(DeleteProductCommand request, CancellationToken cancellationToken)
            {
                Domain.Entities.Product? product = await productRepository.GetByIdAsync(request.Id, cancellationToken);
                productBusinessRules.ProductShouldExistWhenSelected(product);

                await productRepository.DeleteAsync(product, cancellationToken);
            }
        }
    }
}
