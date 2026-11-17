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
            // false -> kalemlerdeki ürünlerden biri bulunamadı.
            var created = await mediator.Send(createOrderCommand, cancellationToken);
            return created ? Ok() : NotFound();
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
            var order = await mediator.Send(new GetByIdOrderQuery { Id = id }, cancellationToken);
            return order is null ? NotFound() : Ok(order);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateOrderCommand updateOrderCommand, CancellationToken cancellationToken)
        {
            // Id route'tan gelir; gövdedeki Id'ye güvenmiyoruz.
            updateOrderCommand.Id = id;
            var updated = await mediator.Send(updateOrderCommand, cancellationToken);
            return updated ? NoContent() : NotFound();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var deleted = await mediator.Send(new DeleteOrderCommand { Id = id }, cancellationToken);
            return deleted ? NoContent() : NotFound();
        }
    }
}
