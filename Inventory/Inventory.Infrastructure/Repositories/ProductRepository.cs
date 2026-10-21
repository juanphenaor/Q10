using Inventory.Application.Contracts;
using Inventory.Domain.Models;
using Inventory.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Repositories
{
    public class ProductRepository(InventoryDbContext context) : IProductRepository
    {
        public async Task<IReadOnlyList<Product>> GetBySkusAsync(IReadOnlyCollection<string> skus, CancellationToken cancellationToken)
        {
            var requestedSkus = skus.Distinct(StringComparer.Ordinal).ToArray();

            return await context.Products
                .AsNoTracking()
                .Where(product => requestedSkus.Contains(product.Sku))
                .ToListAsync(cancellationToken);
        }
    }
}