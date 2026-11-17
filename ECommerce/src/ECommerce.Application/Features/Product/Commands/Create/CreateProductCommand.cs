using ECommerce.Domain.Entities;
using ECommerce.Persistence.Contexts;
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
            private readonly UludagDbContext uludagDbContext;
            public CreateProductCommandHandler(UludagDbContext uludagDbContext)
            {
                this.uludagDbContext = uludagDbContext;
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

                await uludagDbContext.Products.AddAsync(product);
                await uludagDbContext.SaveChangesAsync();
            }
        }
    }
}
