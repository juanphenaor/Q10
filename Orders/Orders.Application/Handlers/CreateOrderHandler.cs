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
            var inventoryResponse = await inventoryClient.CheckAvailabilityAsync(request.Items, cancellationToken);

            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerId = request.CustomerId,
                CreatedAt = DateTime.UtcNow,
                Status = OrderStatus.Pending,
                Items = request.Items.Select(item =>
                {
                    var inventoryItem = inventoryResponse.Items.FirstOrDefault(product => product.Sku == item.Sku)
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
