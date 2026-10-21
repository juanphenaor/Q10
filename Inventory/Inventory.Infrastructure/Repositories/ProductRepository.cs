
using Inventory.Application.Contracts;
using Inventory.Application.Events;
using Inventory.Domain.Models;
using Inventory.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Inventory.Infrastructure.Repositories
{
    public class ProductRepository(InventoryDbContext context) : IProductRepository
    {
        public async Task<bool> AllSkusExistAsync(IReadOnlyCollection<string> skus, CancellationToken cancellationToken)
        {
            var requestedSkus = skus.Distinct(StringComparer.Ordinal).ToArray();
            var existingCount = await context.Products
                .Where(product => requestedSkus.Contains(product.Sku))
                .Select(product => product.Sku)
                .Distinct()
                .CountAsync(cancellationToken);

            return existingCount == requestedSkus.Length;
        }

        public async Task<IReadOnlyList<Product>> GetBySkusAsync(IReadOnlyCollection<string> skus, CancellationToken cancellationToken)
        {
            var requestedSkus = skus
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            return await context.Products
                .AsNoTracking()
                .Where(product => requestedSkus.Contains(product.Sku))
                .ToListAsync(cancellationToken);
        }

        public async Task<StockReservationResult> ReserveStockAsync(OrderCreated order, CancellationToken cancellationToken)
        {
            if (order.OrderId == Guid.Empty ||
                order.Items is null ||
                order.Items.Count == 0 ||
                order.Items.Any(item =>
                    item is null ||
                    string.IsNullOrWhiteSpace(item.Sku) ||
                    item.Quantity <= 0) ||
                order.Items
                    .GroupBy(item => item.Sku, StringComparer.Ordinal)
                    .Any(group => group.Count() > 1))
            {
                return Reject(
                    order.OrderId,
                    "INVALID_REQUEST",
                    "The order contains invalid or duplicate items.");
            }

            var existingOrder = await context.ProcessedOrders
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    processedOrder => processedOrder.OrderId == order.OrderId,
                    cancellationToken);

            if (existingOrder is not null)
            {
                return DeserializeResult(existingOrder.Result);
            }

            await using var transaction =
                await context.Database.BeginTransactionAsync(cancellationToken);

            var lockKey = BitConverter.ToInt64(
                order.OrderId.ToByteArray(), 0);

            await context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock({lockKey})",
                cancellationToken);

            existingOrder = await context.ProcessedOrders
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    processedOrder => processedOrder.OrderId == order.OrderId,
                    cancellationToken);

            if (existingOrder is not null)
            {
                await transaction.CommitAsync(cancellationToken);
                return DeserializeResult(existingOrder.Result);
            }

            var skus = order.Items
                .Select(item => item.Sku)
                .ToArray();

            var products = await context.Products
                .FromSqlInterpolated($"""
                    SELECT *
                    FROM "Products"
                    WHERE "Sku" = ANY ({skus})
                    ORDER BY "Sku"
                    FOR UPDATE
                    """)
                .ToListAsync(cancellationToken);

            var productsBySku = products.ToDictionary(
                product => product.Sku,
                StringComparer.Ordinal);

            var missingItem = order.Items.FirstOrDefault(
                item => !productsBySku.ContainsKey(item.Sku));

            var insufficientItem = order.Items.FirstOrDefault(
                item => productsBySku.TryGetValue(item.Sku, out var product) &&
                        product.AvailableQuantity < item.Quantity);

            StockReservationResult result;

            if (missingItem is not null)
            {
                result = Reject(
                    order.OrderId,
                    "SKU_NOT_FOUND",
                    $"Product SKU {missingItem.Sku} was not found.");
            }
            else if (insufficientItem is not null)
            {
                result = Reject(
                    order.OrderId,
                    "INSUFFICIENT_STOCK",
                    $"Insufficient stock for SKU {insufficientItem.Sku}.");
            }
            else
            {
                foreach (var item in order.Items)
                {
                    productsBySku[item.Sku].AvailableQuantity -= item.Quantity;
                }

                var reservation = new StockReserved
                {
                    OrderId = order.OrderId,
                    Items = order.Items.Select(item => new StockReservedItem
                    {
                        Sku = item.Sku,
                        Quantity = item.Quantity,
                        UnitPrice = productsBySku[item.Sku].UnitPrice
                    }).ToList()
                };

                result = new StockReservationResult
                {
                    IsAccepted = true,
                    Reservation = reservation
                };
            }

            context.ProcessedOrders.Add(new ProcessedOrder
            {
                OrderId = order.OrderId,
                IsAccepted = result.IsAccepted,
                Result = JsonSerializer.Serialize(result),
                ProcessedAt = DateTime.UtcNow
            });

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return result;
        }

        private static StockReservationResult Reject(Guid orderId, string code,
            string reason)
        {
            return new StockReservationResult
            {
                IsAccepted = false,
                Rejection = new StockRejected
                {
                    OrderId = orderId,
                    Code = code,
                    Reason = reason
                }
            };
        }

        private static StockReservationResult DeserializeResult(string result)
        {
            return JsonSerializer.Deserialize<StockReservationResult>(result)
                ?? throw new InvalidOperationException(
                    "The stored stock reservation result is invalid.");
        }
    }
}
