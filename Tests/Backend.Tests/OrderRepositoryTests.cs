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
    public async Task GetByIdAsync_ReturnsOnlyRequestedOrderWithItemsWithoutTracking()
    {
        await using var context = CreateContext();
        var order = new Order
        {
            Id = Guid.NewGuid(),
            ClientName = "Sample Client",
            CreatedAt = DateTime.UtcNow,
            Status = OrderStatus.Confirmed,
            Items =
            [
                new OrderItem
                {
                    Id = Guid.NewGuid(),
                    Sku = "SKU-1",
                    Quantity = 2,
                    UnitPrice = 12.50m
                }
            ]
        };
        context.Orders.AddRange(order, new Order
        {
            Id = Guid.NewGuid(),
            ClientName = "Other Client",
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var result = await new OrderRepository(context).GetByIdAsync(order.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(order.Id, result.Id);
        Assert.Equal(order.ClientName, result.ClientName);
        Assert.Equal(order.CreatedAt, result.CreatedAt);
        Assert.Equal(order.Status, result.Status);
        var item = Assert.Single(result.Items);
        Assert.Equal(order.Items[0].Id, item.Id);
        Assert.Equal("SKU-1", item.Sku);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(12.50m, item.UnitPrice);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task GetByIdAsync_AndHandler_ReturnNullWhenOrderDoesNotExist()
    {
        await using var context = CreateContext();
        var repository = new OrderRepository(context);
        var id = Guid.NewGuid();

        Assert.Null(await repository.GetByIdAsync(id, CancellationToken.None));
        Assert.Null(await new GetOrderByIdHandler(repository).HandleAsync(id, CancellationToken.None));
    }

    [Fact]
    public async Task GetOrderByIdHandler_ReturnsSameDetailsAsList()
    {
        await using var context = CreateContext();
        var repository = new OrderRepository(context);
        var id = await new CreateOrderHandler(repository).HandleAsync(
            new CreateOrderRequest
            {
                ClientName = "Sample Client",
                Items =
                [
                    new OrderItemRequest { Sku = "SKU-1", Quantity = 2 },
                    new OrderItemRequest { Sku = "SKU-2", Quantity = 1 }
                ]
            },
            CancellationToken.None);
        context.ChangeTracker.Clear();

        var result = await new GetOrderByIdHandler(repository).HandleAsync(id, CancellationToken.None);
        var listedOrder = Assert.Single(await new GetOrdersHandler(repository).HandleAsync(CancellationToken.None));

        Assert.NotNull(result);
        Assert.Equal(listedOrder.Id, result.Id);
        Assert.Equal(listedOrder.ClientName, result.ClientName);
        Assert.Equal(listedOrder.CreatedAt, result.CreatedAt);
        Assert.Equal(listedOrder.Status, result.Status);
        Assert.Equal(listedOrder.Items, result.Items);
    }

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
