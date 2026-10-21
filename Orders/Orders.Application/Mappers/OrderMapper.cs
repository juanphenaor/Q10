using Orders.Application.DTOs;
using Orders.Domain.Models;

namespace Orders.Application.Mappers;

public static class OrderMapper
{
    public static OrderResponse ToResponse(Order order)
    {
        return new OrderResponse(
            order.Id,
            order.ClientName,
            new DateTimeOffset(DateTime.SpecifyKind(order.CreatedAt, DateTimeKind.Utc)),
            order.Status.ToString(),
            order.Items
                .OrderBy(item => item.Id)
                .Select(item => new OrderItemResponse(
                    item.Id,
                    item.Sku,
                    item.Quantity,
                    item.UnitPrice))
                .ToArray());
    }
}
