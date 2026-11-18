using ECommerce.Application.Features.Order.Commands.Create;
using ECommerce.Application.Features.Order.Commands.Delete;
using ECommerce.Application.Features.Order.Commands.Update;
using ECommerce.Application.Features.Order.Queries.GetAll;
using ECommerce.Application.Features.Order.Queries.GetById;
using Microsoft.AspNetCore.Mvc;
using Uludag.Core.Mediation;

namespace ECommerce.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController(IMediator mediator) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOrderCommand createOrderCommand, CancellationToken cancellationToken)
        {
            // Bulunamayan kayıt -> 404, kural ihlali -> 400 (BusinessExceptionMiddleware).
            await mediator.Send(createOrderCommand, cancellationToken);
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var orders = await mediator.Send(new GetAllOrderQuery(), cancellationToken);
            return Ok(orders);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            // Kayıt yoksa handler NotFoundException fırlatır -> 404.
            var order = await mediator.Send(new GetByIdOrderQuery { Id = id }, cancellationToken);
            return Ok(order);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateOrderCommand updateOrderCommand, CancellationToken cancellationToken)
        {
            // Id route'tan gelir; gövdedeki Id'ye güvenmiyoruz.
            updateOrderCommand.Id = id;
            await mediator.Send(updateOrderCommand, cancellationToken);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            await mediator.Send(new DeleteOrderCommand { Id = id }, cancellationToken);
            return NoContent();
        }
    }
}
