using ECommerce.Application.Exceptions;
using ECommerce.Application.Features.Product.Rules;
using ECommerce.Application.Services.Repositories;
using NSubstitute;

namespace ECommerce.Application.UnitTests.Features.Product.Rules
{
    // BR-6: Ürün adı benzersizdir (docs/domain.md).
    public class ProductBusinessRulesTests
    {
        private const string ProductName = "Kalem";

        private readonly IProductRepository productRepository = Substitute.For<IProductRepository>();
        private readonly ProductBusinessRules productBusinessRules;

        public ProductBusinessRulesTests()
        {
            this.productBusinessRules = new ProductBusinessRules(productRepository);
        }

        [Fact]
        public async Task ProductNameCannotExist_NameExists_ThrowsBusinessException()
        {
            productRepository
                .GetByNameAsync(ProductName, Arg.Any<CancellationToken>())
                .Returns(new Domain.Entities.Product { Id = Guid.NewGuid(), Name = ProductName });

            await Assert.ThrowsAsync<BusinessException>(() => productBusinessRules.ProductNameCannotExist(ProductName));
        }

        [Fact]
        public async Task ProductNameCannotExist_NameFree_DoesNotThrow()
        {
            productRepository
                .GetByNameAsync(ProductName, Arg.Any<CancellationToken>())
                .Returns((Domain.Entities.Product?)null);

            Exception? exception = await Record.ExceptionAsync(() => productBusinessRules.ProductNameCannotExist(ProductName));

            Assert.Null(exception);
            await productRepository.Received(1).GetByNameAsync(ProductName, Arg.Any<CancellationToken>());
        }
    }
}
