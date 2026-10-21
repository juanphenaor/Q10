namespace Orders.Application.DTOs;

public sealed record OrderItemResponse(Guid Id, string Sku, int Quantity, decimal UnitPrice);
