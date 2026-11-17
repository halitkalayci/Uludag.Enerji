using ECommerce.Application.Features.OrderItem.Commands.Create;
using ECommerce.Application.Features.OrderItem.Commands.Delete;
using ECommerce.Application.Features.OrderItem.Commands.Update;
using ECommerce.Application.Features.OrderItem.Queries.GetAll;
using ECommerce.Application.Features.OrderItem.Queries.GetById;
using Microsoft.AspNetCore.Mvc;
using Uludag.Core.Mediation;

namespace ECommerce.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderItemsController(IMediator mediator) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOrderItemCommand createOrderItemCommand, CancellationToken cancellationToken)
        {
            // false -> sipariş ya da ürün bulunamadı.
            var created = await mediator.Send(createOrderItemCommand, cancellationToken);
            return created ? Ok() : NotFound();
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var orderItems = await mediator.Send(new GetAllOrderItemQuery(), cancellationToken);
            return Ok(orderItems);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var orderItem = await mediator.Send(new GetByIdOrderItemQuery { Id = id }, cancellationToken);
            return orderItem is null ? NotFound() : Ok(orderItem);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateOrderItemCommand updateOrderItemCommand, CancellationToken cancellationToken)
        {
            // Id route'tan gelir; gövdedeki Id'ye güvenmiyoruz.
            updateOrderItemCommand.Id = id;
            var updated = await mediator.Send(updateOrderItemCommand, cancellationToken);
            return updated ? NoContent() : NotFound();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var deleted = await mediator.Send(new DeleteOrderItemCommand { Id = id }, cancellationToken);
            return deleted ? NoContent() : NotFound();
        }
    }
}
