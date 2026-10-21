using Orders.Application.Contracts;
using Orders.Application.DTOs;
using Orders.Application.Events;
using Orders.Domain.Enumerations;
using Orders.Domain.Models;

namespace Orders.Application.Handlers;

public class CreateOrderHandler(IOrderRepository orderRepository)
{
    public async Task<Guid> HandleAsync(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var clientName = request.ClientName?.Trim();
        if (string.IsNullOrWhiteSpace(clientName) || clientName.Length > 200)
        {
            throw new ArgumentException(
                "El nombre del cliente es obligatorio y no puede superar 200 caracteres.",
                nameof(request));
        }

        var items = request.Items
            .GroupBy(item => item.Sku.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => new OrderItemRequest
            {
                Sku = group.Key.ToUpperInvariant(),
                Quantity = group.Sum(item => item.Quantity)
            })
            .ToList();

        var order = new Order
        {
            Id = Guid.NewGuid(),
            ClientName = clientName,
            CreatedAt = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            Items = items.Select(item => new OrderItem
            {
                Id = Guid.NewGuid(),
                Sku = item.Sku,
                Quantity = item.Quantity
            }).ToList()
        };

        var orderCreated = new OrderCreated(order.Id, items.Select(item => new OrderCreatedItem(item.Sku, item.Quantity)).ToList());

        await orderRepository.AddAsync(order, orderCreated, cancellationToken);

        return order.Id;
    }
}