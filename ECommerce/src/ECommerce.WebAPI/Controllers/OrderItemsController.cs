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
            // Bulunamayan kayıt -> 404, kural ihlali -> 400 (BusinessExceptionMiddleware).
            await mediator.Send(createOrderItemCommand, cancellationToken);
            return Ok();
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
            // Kayıt yoksa handler NotFoundException fırlatır -> 404.
            var orderItem = await mediator.Send(new GetByIdOrderItemQuery { Id = id }, cancellationToken);
            return Ok(orderItem);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateOrderItemCommand updateOrderItemCommand, CancellationToken cancellationToken)
        {
            // Id route'tan gelir; gövdedeki Id'ye güvenmiyoruz.
            updateOrderItemCommand.Id = id;
            await mediator.Send(updateOrderItemCommand, cancellationToken);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            await mediator.Send(new DeleteOrderItemCommand { Id = id }, cancellationToken);
            return NoContent();
        }
    }
}
