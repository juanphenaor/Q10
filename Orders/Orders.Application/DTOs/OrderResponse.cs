namespace Orders.Application.DTOs;

public sealed record OrderResponse(Guid Id, Guid CustomerId, DateTimeOffset CreatedAt, string Status, IReadOnlyList<OrderItemResponse> Items);
