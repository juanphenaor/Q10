using Microsoft.EntityFrameworkCore;
using Orders.Application.DTOs;
using Orders.Application.Events;
using Orders.Application.Handlers;
using Orders.Domain.Enumerations;
using Orders.Domain.Models;
using Orders.Infrastructure.Context;
using Orders.Infrastructure.Repositories;
using Xunit;

namespace Backend.Tests;

public sealed class OrderRepositoryTests
{
    [Fact]
    public async Task CreateOrderHandler_AssignsServerValuesAndTrimsClientName()
    {
        await using var context = CreateContext();
        var handler = new CreateOrderHandler(new OrderRepository(context));
        var createdAfter = DateTime.UtcNow;

        var orderId = await handler.HandleAsync(
            new CreateOrderRequest
            {
                ClientName = "  María Pérez  ",
                Items = [new OrderItemRequest { Sku = "SKU-1", Quantity = 2 }]
            },
            CancellationToken.None);

        var order = await context.Orders
            .Include(createdOrder => createdOrder.Items)
            .SingleAsync(createdOrder => createdOrder.Id == orderId);

        Assert.Equal("María Pérez", order.ClientName);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.InRange(order.CreatedAt, createdAfter, DateTime.UtcNow);
        Assert.Single(order.Items);
        Assert.Equal("SKU-1", order.Items[0].Sku);
    }

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
