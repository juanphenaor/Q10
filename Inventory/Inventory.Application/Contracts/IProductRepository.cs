using Inventory.Domain.Models;

namespace Inventory.Application.Contracts
{
    public interface IProductRepository
    {
        Task<IReadOnlyList<Product>> GetBySkusAsync(IReadOnlyCollection<string> skus, CancellationToken cancellationToken);
    }
}