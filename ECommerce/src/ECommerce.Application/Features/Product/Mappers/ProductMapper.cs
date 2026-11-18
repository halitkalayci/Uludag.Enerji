using ECommerce.Application.Features.Product.Commands.Create;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Application.Features.Product.Mappers
{
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
    }
}
