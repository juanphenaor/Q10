using Inventory.Application.Contracts;
using Inventory.Application.DTOs;

namespace Inventory.Application.Handlers;

public sealed class ValidateSkusHandler(IProductRepository productRepository)
{
    public Task<bool> HandleAsync(ValidateSkusRequest request, CancellationToken cancellationToken)
    {
        var skus = request.Skus
            .Select(sku => sku.Trim().ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return productRepository.AllSkusExistAsync(skus, cancellationToken);
    }
}
