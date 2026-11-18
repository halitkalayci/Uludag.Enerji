using ECommerce.Application.Features.Product.Rules;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.Product.Commands.Update
{
    // Ürün bulunamazsa NotFoundException fırlar.
    public class UpdateProductCommand : IRequest
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public int Stock { get; set; }

        public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand>
        {
            // Dependency Injection
            private readonly IProductRepository productRepository;
            private readonly ProductBusinessRules productBusinessRules;
            public UpdateProductCommandHandler(IProductRepository productRepository, ProductBusinessRules productBusinessRules)
            {
                this.productRepository = productRepository;
                this.productBusinessRules = productBusinessRules;
            }

            public async Task Handle(UpdateProductCommand request, CancellationToken cancellationToken)
            {
                Domain.Entities.Product? product = await productRepository.GetByIdAsync(request.Id, cancellationToken);
                productBusinessRules.ProductShouldExistWhenSelected(product);

                // Mapping
                product.Name = request.Name;
                product.Price = new Domain.ValueObjects.Money(request.Amount, request.Currency);
                product.Stock = request.Stock;

                await productRepository.UpdateAsync(product, cancellationToken);
            }
        }
    }
}
