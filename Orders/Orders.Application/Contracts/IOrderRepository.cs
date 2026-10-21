using Orders.Application.Events;
using Orders.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Orders.Application.Contracts
{
    public interface IOrderRepository
    {
        Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken cancellationToken);
        Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task AddAsync(Order order, OrderCreated orderCreated, CancellationToken cancellationToken);
        Task<bool> ConfirmAsync(StockReserved stockReserved, CancellationToken cancellationToken);
        Task<bool> RejectAsync(StockRejected stockRejected, CancellationToken cancellationToken);
    }
}
