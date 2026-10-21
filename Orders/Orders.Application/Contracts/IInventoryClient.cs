using Orders.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Orders.Application.Contracts
{
    public interface IInventoryClient
    {
        Task<InventoryAvailabilityResponse> CheckAvailabilityAsync(IEnumerable<OrderItemRequest> items, CancellationToken cancellationToken);
    }
}
