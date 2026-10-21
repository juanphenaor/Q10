using System.Net;
using Inventory.Application.Contracts;
using Inventory.Application.DTOs;
using Inventory.Application.Exceptions;
using Inventory.Domain.Models;

namespace Inventory.Application.Handlers
{
    public class CheckAvailabilityHandler(IProductRepository productRepository)
    {
        public async Task<AvailabilityResponse> HandleAsync(
            AvailabilityRequest request,
            CancellationToken cancellationToken)
        {
            if (request?.Items is null || request.Items.Count == 0)
            {
                throw InvalidRequest();
            }

            var normalizedSkus = new List<string>(request.Items.Count);

            foreach (var item in request.Items)
            {
                if (item is null || string.IsNullOrWhiteSpace(item.Sku) || item.Quantity <= 0)
                {
                    throw InvalidRequest();
                }

                normalizedSkus.Add(item.Sku.Trim().ToUpperInvariant());
            }

            if (normalizedSkus.Distinct(StringComparer.Ordinal).Count() != normalizedSkus.Count)
            {
                throw InvalidRequest();
            }

            var products = await productRepository.GetBySkusAsync(normalizedSkus, cancellationToken);
            var productsBySku = products.ToDictionary(product => product.Sku, StringComparer.Ordinal);
            var response = new AvailabilityResponse();

            for (var index = 0; index < request.Items.Count; index++)
            {
                var requestedItem = request.Items[index];
                var sku = normalizedSkus[index];

                if (!productsBySku.TryGetValue(sku, out var product))
                {
                    throw new InventoryException(
                        "SKU_NOT_FOUND",
                        $"No existe el producto con SKU {requestedItem.Sku.Trim()}.",
                        (int)HttpStatusCode.Conflict);
                }

                if (product.AvailableQuantity < requestedItem.Quantity)
                {
                    throw new InventoryException(
                        "INSUFFICIENT_STOCK",
                        $"No hay suficientes unidades disponibles para el SKU {requestedItem.Sku.Trim()}.",
                        (int)HttpStatusCode.Conflict);
                }

                response.Items.Add(new AvailabilityItemResponse
                {
                    Sku = requestedItem.Sku.Trim(),
                    UnitPrice = product.UnitPrice
                });
            }

            return response;
        }

        private static InventoryException InvalidRequest() => new(
            "INVALID_REQUEST",
            "La solicitud no es válida.",
            (int)HttpStatusCode.BadRequest);
    }
}