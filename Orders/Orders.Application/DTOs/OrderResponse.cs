namespace Orders.Application.DTOs;

public sealed record OrderResponse(Guid Id, string ClientName, DateTimeOffset CreatedAt, string Status, IReadOnlyList<OrderItemResponse> Items);
