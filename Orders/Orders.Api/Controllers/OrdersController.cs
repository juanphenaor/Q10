using Microsoft.AspNetCore.Mvc;
using Orders.Application.DTOs;
using Orders.Application.Exceptions;
using Orders.Application.Handlers;

namespace Orders.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController(
        CreateOrderHandler handler,
        GetOrdersHandler getOrdersHandler,
        GetOrderByIdHandler getOrderByIdHandler) : ControllerBase
    {
        
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<OrderResponse>>> GetAll(CancellationToken cancellationToken)
        {
            var orders = await getOrdersHandler.HandleAsync(cancellationToken);

            return Ok(orders);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<OrderResponse>> GetById(Guid id, CancellationToken cancellationToken)
        {
            var order = await getOrderByIdHandler.HandleAsync(id, cancellationToken);

            return order is null ? NotFound() : Ok(order);
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<IActionResult> Create(CreateOrderRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var orderId = await handler.HandleAsync(request, cancellationToken);

                return CreatedAtAction(nameof(GetById), new { id = orderId }, new { Id = orderId });
            }
            catch (InventoryException exception)
            {
                return Conflict(new
                {
                    code = exception.Code,
                    message = exception.Message
                });
            }
        }
    }
}
