using ECommerce.Application.Features.Product.Mappers;
using ECommerce.Application.Features.Product.Rules;
using ECommerce.Application.Services.Repositories;
using ECommerce.Application.Features.Product.Constants;
using ECommerce.Application.Services.CategoryService;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.Product.Commands.Create
{
    public class CreateProductCommand : IRequest, ISecuredRequest // komut varolmadan (geriye değer döndürmeyen istek)
    {
        // Yetki: Product.Admin, Product.Write veya Product.Create claim'lerindeexn biri yeterli (global Admin her zaman geçer).
        // Açık (explicit) implementasyon: public property olmaz, JSON gövdesine / Swagger'a girmez.
        string[] ISecuredRequest.Roles => [ProductOperationClaims.Admin, ProductOperationClaims.Write, ProductOperationClaims.Create];

        // İki parçadan oluşur -> Komutun gövdesi, (Create işlemi yapılırken ne istiyosun?)

        public string Name { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public int Stock { get; set; }
        // Ürünün kategorisi (spec 0009); null ya da gönderilmezse ürün kategorisizdir.
        public Guid? CategoryId { get; set; }

        // İkinci parça -> Commandın handleri
        // Bu komut geldiğinde ne olacak?
        // Gövdeyi aldı işlemi yapacak.


        // varolması mantıklı..
        public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand> // Komut handlerı, işleyen kişi.
        {
            // Dependency Injection
            private readonly ProductBusinessRules productBusinessRules;
            private readonly IProductRepository productRepository; // persistence tarafından çözülüyor (sarmallayan)
            // Kategori başka feature'ın entity'si: repository'si değil servisi kullanılır (F-6).
            private readonly ICategoryService categoryService;
            public CreateProductCommandHandler(IProductRepository productRepository, ProductBusinessRules productBusinessRules, ICategoryService categoryService)
            {
                this.productRepository = productRepository;
                this.productBusinessRules = productBusinessRules;
                this.categoryService = categoryService;
            }

            // Bir developerın ekranında en çok olacak nokta.
            public async Task Handle(CreateProductCommand request, CancellationToken cancellationToken)
            {
                // Orchestrator gibi davranır.
                // Doğru sırayla, doğru kişileri çağır.
                // İş kuralı
                await productBusinessRules.ProductNameCannotExist(request.Name);
                await categoryService.CategoryShouldExistWhenGivenAsync(request.CategoryId, cancellationToken);

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
