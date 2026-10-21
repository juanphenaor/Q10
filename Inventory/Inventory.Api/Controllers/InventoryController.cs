using Inventory.Application.DTOs;
using Inventory.Application.Exceptions;
using Inventory.Application.Handlers;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController(CheckAvailabilityHandler handler) : ControllerBase
    {
        [HttpPost("availability")]
        public async Task<ActionResult<AvailabilityResponse>> CheckAvailability([FromBody] AvailabilityRequest request, CancellationToken cancellationToken)
        {
            try
            {
                return Ok(await handler.HandleAsync(request, cancellationToken));
            }
            catch (InventoryException exception)
            {
                return StatusCode(exception.StatusCode, new InventoryErrorResponse
                {
                    Code = exception.Code,
                    Message = exception.Message
                });
            }
        }
    }
}