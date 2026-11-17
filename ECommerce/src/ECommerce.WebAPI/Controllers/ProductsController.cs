using ECommerce.Application.Features.Product.Commands.Create;
using ECommerce.Application.Features.Product.Commands.Delete;
using ECommerce.Application.Features.Product.Commands.Update;
using ECommerce.Application.Features.Product.Queries.GetAll;
using ECommerce.Application.Features.Product.Queries.GetById;
using Microsoft.AspNetCore.Mvc;
using Uludag.Core.Mediation;

namespace ECommerce.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController(IMediator mediator) : ControllerBase
    {
        [HttpPost]
        //                                     REFERANS UNUTMA!
        public async Task<IActionResult> Create([FromBody] CreateProductCommand createProductCommand, CancellationToken cancellationToken)
        {
            // veritabanı bağlantısı -> YANLIŞ
            // iş kodu, validation -> YANLIŞ

            // Sorumluluk -> Kullanıcı ile http üzerinden haberleş -> isteği doğru
            // handler'a ilet.

            // Yanlış bağlantı. -> Handlerı newleme, burdaki iletişimi Mediator'un (ortak iletişim ağı)
            // yapıyor olması.
            // Controller handler'ı tanımaz; isteği Mediator'a verir, Mediator doğru handler'ı bulur.
            await mediator.Send(createProductCommand, cancellationToken);
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var products = await mediator.Send(new GetAllProductQuery(), cancellationToken);
            return Ok(products);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var product = await mediator.Send(new GetByIdProductQuery { Id = id }, cancellationToken);
            return product is null ? NotFound() : Ok(product);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateProductCommand updateProductCommand, CancellationToken cancellationToken)
        {
            // Id route'tan gelir; gövdedeki Id'ye güvenmiyoruz.
            updateProductCommand.Id = id;
            var updated = await mediator.Send(updateProductCommand, cancellationToken);
            return updated ? NoContent() : NotFound();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var deleted = await mediator.Send(new DeleteProductCommand { Id = id }, cancellationToken);
            return deleted ? NoContent() : NotFound();
        }
    }
}
