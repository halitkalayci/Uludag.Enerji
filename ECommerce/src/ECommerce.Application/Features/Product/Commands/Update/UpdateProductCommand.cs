using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.Product.Commands.Update
{
    // Geriye bool döner: true -> güncellendi, false -> ürün bulunamadı.
    public class UpdateProductCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public int Stock { get; set; }

        public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, bool>
        {
            // Dependency Injection
            private readonly IProductRepository productRepository;
            public UpdateProductCommandHandler(IProductRepository productRepository)
            {
                this.productRepository = productRepository;
            }

            public async Task<bool> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
            {
                Domain.Entities.Product? product = await productRepository.GetByIdAsync(request.Id, cancellationToken);

                if (product is null)
                    return false;

                // Mapping
                product.Name = request.Name;
                product.Price = new Domain.ValueObjects.Money(request.Amount, request.Currency);
                product.Stock = request.Stock;

                await productRepository.UpdateAsync(product, cancellationToken);
                return true;
            }
        }
    }
}
