using Inventory.Application.Events;
using Inventory.Domain.Models;

namespace Inventory.Application.Contracts
{
    public interface IProductRepository
    {
        Task<bool> AllSkusExistAsync(IReadOnlyCollection<string> skus, CancellationToken cancellationToken);
        Task<IReadOnlyList<Product>> GetBySkusAsync(IReadOnlyCollection<string> skus, CancellationToken cancellationToken);
        Task<StockReservationResult> ReserveStockAsync(OrderCreated order, CancellationToken cancellationToken);
    }
}