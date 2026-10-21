using Inventory.Application.DTOs;
using Inventory.Application.Handlers;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers;

[ApiController]
[Route("api/inventory")]
public sealed class InventoryController(ValidateSkusHandler handler) : ControllerBase
{
    /// <summary>Checks whether all supplied SKUs exist without reserving stock.</summary>
    [HttpPost("validate-skus")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<bool>> ValidateSkus(
        ValidateSkusRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(request, cancellationToken));
    }
}
