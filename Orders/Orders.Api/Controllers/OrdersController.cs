using Microsoft.AspNetCore.Mvc;
using Orders.Application.DTOs;
using Orders.Application.Exceptions;
using Orders.Application.Handlers;

namespace Orders.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController(CreateOrderHandler handler, GetOrdersHandler getOrdersHandler) : ControllerBase
    {
        
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<OrderResponse>>> GetAll(CancellationToken cancellationToken)
        {
            var orders = await getOrdersHandler.HandleAsync(cancellationToken);

            return Ok(orders);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateOrderRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var orderId = await handler.HandleAsync(request, cancellationToken);

                return Ok(new { Id = orderId });
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
