
using System.Net.Http.Json;
using Orders.Application.Contracts;
using Orders.Application.DTOs;
using Orders.Application.Exceptions;

namespace Orders.Infrastructure.Clients;

public class InventoryClient(HttpClient httpClient) : IInventoryClient
{
    public async Task<InventoryAvailabilityResponse> CheckAvailabilityAsync(
        IEnumerable<OrderItemRequest> items,
        CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/inventory/availability",
            new { Items = items },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<InventoryErrorResponse>(
                cancellationToken);

            if (error is null)
            {
                throw new HttpRequestException(
                    $"Inventory respondió con HTTP {(int)response.StatusCode}.");
            }

            throw new InventoryException(error.Code, error.Message);
        }

        return await response.Content.ReadFromJsonAsync<InventoryAvailabilityResponse>(
            cancellationToken)
            ?? throw new InvalidOperationException(
                "Inventory devolvió una respuesta vacía o inválida.");
    }
}
