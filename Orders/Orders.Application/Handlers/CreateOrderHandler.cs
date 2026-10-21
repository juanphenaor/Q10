using Orders.Application.Contracts;
using Orders.Application.DTOs;
using Orders.Application.Exceptions;
using Orders.Domain.Enumerations;
using Orders.Domain.Models;

namespace Orders.Application.Handlers
{
    public class CreateOrderHandler(IInventoryClient inventoryClient, IOrderRepository orderRepository)
    {
        public async Task<Guid> HandleAsync(CreateOrderRequest request, CancellationToken cancellationToken)
        {
            if (request.CustomerId == Guid.Empty)
            {
                throw new ArgumentException("El identificador del cliente no puede estar vacío.");
            }

            var items = request.Items
                .Select(item => new OrderItemRequest
                {
                    Sku = item.Sku.Trim(),
                    Quantity = item.Quantity
                })
                .ToList();

            var inventoryResponse = await inventoryClient.CheckAvailabilityAsync(items, cancellationToken);

            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerId = request.CustomerId,
                CreatedAt = DateTime.UtcNow,
                Status = OrderStatus.Pending,
                Items = items.Select(item =>
                {
                    var inventoryItem = inventoryResponse.Items.FirstOrDefault(product => string.Equals(product.Sku, item.Sku, StringComparison.OrdinalIgnoreCase))
                        ?? throw new InvalidOperationException($"Inventory no devolvió el SKU solicitado: {item.Sku}");

                    return new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        Sku = item.Sku,
                        Quantity = item.Quantity,
                        UnitPrice = inventoryItem.UnitPrice
                    };
                }).ToList()
            };

            await orderRepository.AddAsync(order, cancellationToken);

            return order.Id;
        }
    }
}
