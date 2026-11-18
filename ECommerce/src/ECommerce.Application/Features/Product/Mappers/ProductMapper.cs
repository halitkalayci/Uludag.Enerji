using ECommerce.Application.Features.Product.Commands.Create;
using ECommerce.Application.Features.Product.Commands.Update;
using ECommerce.Application.Features.Product.Queries;

namespace ECommerce.Application.Features.Product.Mappers
{
    // Product feature'ının tüm mapping'leri burada yazılır.
    // Command/Query handler'ları mapping yapmaz; bu metotları çağırır.
    public static class ProductMapper
    {
        public static Domain.Entities.Product MapCreateCommandToProduct(CreateProductCommand command)
        {
            // Yeni bir instance oluşturma.
            return new Domain.Entities.Product
            {
                Name = command.Name,
                Price = new Domain.ValueObjects.Money(command.Amount, command.Currency),
                Stock = command.Stock
            };
        }

        // Var olan (DB'den gelen, EF tarafından takip edilen) entity'nin üzerine yazar; yeni instance oluşturmaz.
        public static void MapUpdateCommandToProduct(UpdateProductCommand command, Domain.Entities.Product product)
        {
            product.Name = command.Name;
            product.Price = new Domain.ValueObjects.Money(command.Amount, command.Currency);
            product.Stock = command.Stock;
        }

        public static ProductResponse MapProductToResponse(Domain.Entities.Product product)
        {
            return new ProductResponse
            {
                Id = product.Id,
                Name = product.Name,
                Amount = product.Price.Amount,
                Currency = product.Price.Currency,
                Stock = product.Stock
            };
        }

        public static List<ProductResponse> MapProductsToResponses(List<Domain.Entities.Product> products)
        {
            return products.Select(MapProductToResponse).ToList();
        }
    }
}
