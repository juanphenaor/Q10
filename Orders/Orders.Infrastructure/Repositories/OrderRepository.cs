using Microsoft.EntityFrameworkCore;
using Orders.Application.Contracts;
using Orders.Application.Events;
using Orders.Domain.Enumerations;
using Orders.Domain.Models;
using Orders.Infrastructure.Context;
using Orders.Infrastructure.Models;
using System.Text.Json;

namespace Orders.Infrastructure.Repositories
{
    public class OrderRepository(OrdersDbContext context) : IOrderRepository
    {
        public async Task AddAsync(Order order, OrderCreated orderCreated, CancellationToken cancellationToken)
        {
            await context.OutboxMessages.AddAsync(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = "order.created",
                Payload = JsonSerializer.Serialize(orderCreated),
                OccurredAt = DateTime.UtcNow
            }, cancellationToken);

            await context.Orders.AddAsync(order, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> ConfirmAsync(StockReserved stockReserved, CancellationToken cancellationToken)
        {
            var order = await context.Orders
                .Include(order => order.Items)
                .SingleOrDefaultAsync(
                    order => order.Id == stockReserved.OrderId,
                    cancellationToken);

            if (order is null || order.Status != OrderStatus.Pending)
            {
                return false;
            }

            if (stockReserved.Items is null ||
                stockReserved.Items.Count != order.Items.Count ||
                stockReserved.Items.GroupBy(item => item.Sku, StringComparer.Ordinal).Any(group => group.Count() > 1) ||
                order.Items.Any(orderItem => !stockReserved.Items.Any(reservedItem => reservedItem.Sku == orderItem.Sku && reservedItem.Quantity == orderItem.Quantity && reservedItem.UnitPrice >= 0)) ||
                stockReserved.Items.Any(reservedItem => !order.Items.Any(orderItem => orderItem.Sku == reservedItem.Sku)))
            {
                throw new InvalidOperationException(
                    $"The stock reservation does not match order {order.Id}.");
            }

            foreach (var orderItem in order.Items)
            {
                var reservedItem = stockReserved.Items.Single(
                    item => item.Sku == orderItem.Sku);

                orderItem.UnitPrice = reservedItem.UnitPrice;
            }

            order.Status = OrderStatus.Confirmed;

            await context.SaveChangesAsync(cancellationToken);

            return true;
        }

        public async Task<bool> RejectAsync(StockRejected stockRejected, CancellationToken cancellationToken)
        {
            var order = await context.Orders
                .SingleOrDefaultAsync(
                    order => order.Id == stockRejected.OrderId,
                    cancellationToken);

            if (order is null || order.Status != OrderStatus.Pending)
            {
                return false;
            }

            order.Status = OrderStatus.Rejected;

            await context.SaveChangesAsync(cancellationToken);

            return true;
        }

    }
}
