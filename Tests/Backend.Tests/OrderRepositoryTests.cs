using Microsoft.EntityFrameworkCore;
using Orders.Application.Events;
using Orders.Domain.Enumerations;
using Orders.Domain.Models;
using Orders.Infrastructure.Context;
using Orders.Infrastructure.Repositories;
using Xunit;

namespace Backend.Tests;

public sealed class OrderRepositoryTests
{
    [Fact]
    public async Task ConfirmAsync_ConfirmsPendingOrderAndCannotConfirmItAgain()
    {
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            Id = orderId,
            ClientName = "María Pérez",
            CreatedAt = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            Items =
            [
                new OrderItem
                {
                    Id = Guid.NewGuid(),
                    Sku = "SKU-1",
                    Quantity = 2
                }
            ]
        };

        await using var context = CreateContext();
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var repository = new OrderRepository(context);
        var reservation = new StockReserved(
            orderId,
            [new StockReservedItem("SKU-1", 2, 12.50m)]);

        Assert.True(await repository.ConfirmAsync(reservation, CancellationToken.None));
        Assert.Equal(OrderStatus.Confirmed, order.Status);
        Assert.Equal(12.50m, order.Items[0].UnitPrice);
        Assert.False(await repository.ConfirmAsync(reservation, CancellationToken.None));
    }

    [Fact]
    public async Task RejectAsync_RejectsPendingOrderAndPreventsConfirmation()
    {
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            Id = orderId,
            ClientName = "María Pérez",
            CreatedAt = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            Items =
            [
                new OrderItem
                {
                    Id = Guid.NewGuid(),
                    Sku = "SKU-1",
                    Quantity = 2
                }
            ]
        };

        await using var context = CreateContext();
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var repository = new OrderRepository(context);

        Assert.True(await repository.RejectAsync(
            new StockRejected(orderId, "INSUFFICIENT_STOCK", "Not enough stock."),
            CancellationToken.None));
        Assert.Equal(OrderStatus.Rejected, order.Status);
        Assert.False(await repository.ConfirmAsync(
            new StockReserved(orderId, [new StockReservedItem("SKU-1", 2, 12.50m)]),
            CancellationToken.None));
    }

    private static OrdersDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<OrdersDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new OrdersDbContext(options);
    }
}
