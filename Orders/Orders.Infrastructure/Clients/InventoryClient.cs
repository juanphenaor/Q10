using Orders.Application.Contracts;
using Orders.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;
using static Orders.Application.Exceptions.InsufficientInventoryException;

namespace Orders.Infrastructure.Clients
{
    public class InventoryClient(HttpClient httpClient) : IInventoryClient
    {
        public async Task<InventoryAvailabilityResponse> CheckAvailabilityAsync(IEnumerable<OrderItemRequest> items, CancellationToken cancellationToken)
        {
            var response = await httpClient.PostAsJsonAsync(
                "api/inventory/availability",
                new { Items = items },
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content
                    .ReadFromJsonAsync<InventoryErrorResponse>(
                        cancellationToken);

                if (error is not null)
                {
                    throw new InventoryException(error.Code, error.Message);
                }

                response.EnsureSuccessStatusCode();
            }

            var result = await response.Content.ReadFromJsonAsync<AvailabilityResponse>(
                cancellationToken);

            return await response.Content.ReadFromJsonAsync<InventoryAvailabilityResponse>(cancellationToken)
                ?? throw new InvalidOperationException("Inventory devolvió una respuesta vacía o inválida.");
        }

        private sealed class AvailabilityResponse
        {
            public bool Available { get; set; }
        }
    }
}