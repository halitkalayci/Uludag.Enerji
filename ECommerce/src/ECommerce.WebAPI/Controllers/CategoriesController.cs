using ECommerce.Application.Features.Category.Commands.Create;
using ECommerce.Application.Features.Category.Commands.Delete;
using ECommerce.Application.Features.Category.Commands.Update;
using ECommerce.Application.Features.Category.Queries.GetAll;
using ECommerce.Application.Features.Category.Queries.GetById;
using Microsoft.AspNetCore.Mvc;
using Uludag.Core.Mediation;

namespace ECommerce.WebAPI.Controllers
{
    // uludag-gen ile üretildi (spec 0005); serbestçe düzenlenebilir.
    // Controller iş mantığı içermez; isteği Mediator'a verir, Mediator doğru handler'ı bulur.
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController(IMediator mediator) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCategoryCommand createCategoryCommand, CancellationToken cancellationToken)
        {
            await mediator.Send(createCategoryCommand, cancellationToken);
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var categoriesResponse = await mediator.Send(new GetAllCategoryQuery(), cancellationToken);
            return Ok(categoriesResponse);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            // Kayıt yoksa handler NotFoundException fırlatır -> 404.
            var categoryResponse = await mediator.Send(new GetByIdCategoryQuery { Id = id }, cancellationToken);
            return Ok(categoryResponse);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateCategoryCommand updateCategoryCommand, CancellationToken cancellationToken)
        {
            // Id route'tan gelir; gövdedeki Id'ye güvenilmez.
            updateCategoryCommand.Id = id;
            await mediator.Send(updateCategoryCommand, cancellationToken);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            await mediator.Send(new DeleteCategoryCommand { Id = id }, cancellationToken);
            return NoContent();
        }
    }
}
