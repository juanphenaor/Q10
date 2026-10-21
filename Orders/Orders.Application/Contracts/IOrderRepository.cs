using Orders.Application.Events;
using Orders.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Orders.Application.Contracts
{
    public interface IOrderRepository
    {
        Task AddAsync(Order order, OrderCreated orderCreated, CancellationToken cancellationToken);
    }
}
