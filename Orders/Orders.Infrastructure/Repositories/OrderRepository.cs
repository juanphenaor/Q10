using Orders.Application.Contracts;
using Orders.Application.Events;
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
    }
}
