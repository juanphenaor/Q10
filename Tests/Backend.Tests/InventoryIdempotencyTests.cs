using Inventory.Application.Events;
using Inventory.Domain.Models;
using Inventory.Infrastructure.Context;
using Inventory.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace Backend.Tests;

public sealed class InventoryIdempotencyTests
{
    private const string ConnectionStringVariable = "TEST_INVENTORY_DATABASE";

    [Fact]
    public async Task ReserveStockAsync_ReprocessingOrderReturnsStoredResultWithoutDeductingStockAgain()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"The {ConnectionStringVariable} environment variable is required. Run `docker compose --profile test run --rm backend-tests`.");
        }

        var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString);

        if (!string.Equals(connectionStringBuilder.Host, "inventory-db", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(connectionStringBuilder.Database, "InventoryDb", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"The {ConnectionStringVariable} connection must target the InventoryDb database on the inventory-db Docker Compose service.");
        }

        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var context = new InventoryDbContext(options);
        await context.Database.MigrateAsync();

        var orderId = Guid.NewGuid();
        var sku = $"IDEMPOTENCY-{Guid.NewGuid():N}";

        context.Products.Add(new Product
        {
            Id = Guid.NewGuid(),
            Sku = sku,
            UnitPrice = 3.25m,
            AvailableQuantity = 10
        });
        await context.SaveChangesAsync();

        try
        {
            var order = new OrderCreated
            {
                OrderId = orderId,
                Items = [new OrderCreatedItem { Sku = sku, Quantity = 3 }]
            };
            var repository = new ProductRepository(context);

            var firstResult = await repository.ReserveStockAsync(order, CancellationToken.None);
            var replayResult = await repository.ReserveStockAsync(order, CancellationToken.None);

            Assert.True(firstResult.IsAccepted);
            Assert.True(replayResult.IsAccepted);
            Assert.Equal(firstResult.Reservation!.Items, replayResult.Reservation!.Items);
            Assert.Equal(
                7,
                await context.Products
                    .Where(product => product.Sku == sku)
                    .Select(product => product.AvailableQuantity)
                    .SingleAsync());
        }
        finally
        {
            context.ProcessedOrders.RemoveRange(
                context.ProcessedOrders.Where(processedOrder => processedOrder.OrderId == orderId));
            context.Products.RemoveRange(context.Products.Where(product => product.Sku == sku));
            await context.SaveChangesAsync();
        }
    }
}
