using ECommerce.Application.Features.Product.Mappers;
using ECommerce.Application.Features.Product.Rules;
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
            private readonly ProductBusinessRules productBusinessRules;
            private readonly IProductRepository productRepository; // persistence tarafından çözülüyor (sarmallayan)
            public CreateProductCommandHandler(IProductRepository productRepository, ProductBusinessRules productBusinessRules)
            {
                this.productRepository = productRepository;
                this.productBusinessRules = productBusinessRules;
            }

            // Bir developerın ekranında en çok olacak nokta.
            public async Task Handle(CreateProductCommand request, CancellationToken cancellationToken)
            {
                // Orchestrator gibi davranır.
                // Doğru sırayla, doğru kişileri çağır.
                // İş kuralı
                await productBusinessRules.ProductNameCannotExist(request.Name);

                // İşi kendisi yapıyor.
                // Mapper -> Gibi davranmadı
                // Mapper'ım olsa, onu çağırsam.
                var product = ProductMapper.MapCreateCommandToProduct(request);

                // Veritabanı
                await productRepository.AddAsync(product, cancellationToken);
            } 
        }
    }
}
