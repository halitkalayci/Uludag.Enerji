using Uludag.Generator.Generation;
using static Uludag.Generator.Templates.TemplateText;

namespace Uludag.Generator.Templates
{
    // ECommerce.WebAPI şablonu. Kalıp: ProductsController. Controller yalnızca IMediator kullanır (F-5).
    internal static class WebApiTemplates
    {
        public static string Controller(CrudModel m)
        {
            string ns = $"ECommerce.Application.Features.{m.Entity}";
            return $$"""
                using {{ns}}.Commands.Create;
                using {{ns}}.Commands.Delete;
                using {{ns}}.Commands.Update;
                using {{ns}}.Queries.GetAll;
                using {{ns}}.Queries.GetById;
                using Microsoft.AspNetCore.Mvc;
                using Uludag.Core.Mediation;

                namespace ECommerce.WebAPI.Controllers
                {
                    // {{GeneratedNote}}
                    // Controller iş mantığı içermez; isteği Mediator'a verir, Mediator doğru handler'ı bulur.
                    [Route("api/[controller]")]
                    [ApiController]
                    public class {{m.Plural}}Controller(IMediator mediator) : ControllerBase
                    {
                        [HttpPost]
                        public async Task<IActionResult> Create([FromBody] Create{{m.Entity}}Command create{{m.Entity}}Command, CancellationToken cancellationToken)
                        {
                            await mediator.Send(create{{m.Entity}}Command, cancellationToken);
                            return Ok();
                        }

                        [HttpGet]
                        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
                        {
                            var {{m.PluralVar.TrimStart('@')}}Response = await mediator.Send(new GetAll{{m.Entity}}Query(), cancellationToken);
                            return Ok({{m.PluralVar.TrimStart('@')}}Response);
                        }

                        [HttpGet("{id:guid}")]
                        public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
                        {
                            // Kayıt yoksa handler NotFoundException fırlatır -> 404.
                            var {{m.Camel}}Response = await mediator.Send(new GetById{{m.Entity}}Query { Id = id }, cancellationToken);
                            return Ok({{m.Camel}}Response);
                        }

                        [HttpPut("{id:guid}")]
                        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] Update{{m.Entity}}Command update{{m.Entity}}Command, CancellationToken cancellationToken)
                        {
                            // Id route'tan gelir; gövdedeki Id'ye güvenilmez.
                            update{{m.Entity}}Command.Id = id;
                            await mediator.Send(update{{m.Entity}}Command, cancellationToken);
                            return NoContent();
                        }

                        [HttpDelete("{id:guid}")]
                        public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
                        {
                            await mediator.Send(new Delete{{m.Entity}}Command { Id = id }, cancellationToken);
                            return NoContent();
                        }
                    }
                }
                """;
        }
    }
}
