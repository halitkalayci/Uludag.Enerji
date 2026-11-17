using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.Product.Commands.Create
{
    public class CreateProductCommand : IRequest // komut varolmadan (geriye değer döndürmeyen istek)
    {
        // İki parçadan oluşur -> Komutun gövdesi, (Create işlemi yapılırken ne istiyosun?)

        public string Name { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public int Stock { get; set; }

        // İkinci parça -> Commandın handleri
        // Bu komut geldiğinde ne olacak?
        // Gövdeyi aldı işlemi yapacak.


        // varolması mantıklı..
        public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand> // Komut handlerı, işleyen kişi.
        {
            // Dependency Injection
            private readonly IProductRepository productRepository; // persistence tarafından çözülüyor (sarmallayan)
            public CreateProductCommandHandler(IProductRepository productRepository)
            {
                this.productRepository = productRepository;
            }

            public async Task Handle(CreateProductCommand request, CancellationToken cancellationToken)
            {
                // Mapping
                Domain.Entities.Product product = new()
                {
                    Name = request.Name,
                    Price = new Domain.ValueObjects.Money(request.Amount, request.Currency),
                    Stock = request.Stock
                };

                await productRepository.AddAsync(product, cancellationToken);
            } 
        }
    }
}
