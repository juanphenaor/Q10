using Orders.Application.Contracts;
using Orders.Domain.Models;
using Orders.Infrastructure.Context;
using System;
using System.Collections.Generic;
using System.Text;

namespace Orders.Infrastructure.Repositories
{
    public class OrderRepository(OrdersDbContext context) : IOrderRepository
    {
        public async Task AddAsync(Order order, CancellationToken cancellationToken)
        {
            await context.Orders.AddAsync(order, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
