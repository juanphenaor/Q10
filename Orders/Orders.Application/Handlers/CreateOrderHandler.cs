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
        if (request.CustomerId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del cliente no puede estar vacío.");
        }

        var items = request.Items
            .Select(item => new OrderItemRequest
            {
                Sku = item.Sku.Trim(),
                Quantity = item.Quantity
            })
            .ToList();

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            CreatedAt = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            Items = items.Select(item => new OrderItem
            {
                Id = Guid.NewGuid(),
                Sku = item.Sku,
                Quantity = item.Quantity
            }).ToList()
        };

        var orderCreated = new OrderCreated
        {
            OrderId = order.Id,
            Items = items.Select(item => new OrderCreatedItem
            {
                Sku = item.Sku,
                Quantity = item.Quantity
            }).ToList()
        };

        await orderRepository.AddAsync(order, orderCreated, cancellationToken);

        return order.Id;
    }
}