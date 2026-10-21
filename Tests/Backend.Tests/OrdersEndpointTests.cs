using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orders.Api.Controllers;
using Orders.Application.Contracts;
using Orders.Application.DTOs;
using Orders.Application.Handlers;
using Orders.Infrastructure.Context;
using Orders.Infrastructure.Repositories;
using Xunit;

namespace Backend.Tests;

public sealed class OrdersEndpointTests : IAsyncLifetime
{
    private WebApplication app = null!;
    private HttpClient client = null!;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddControllers().AddApplicationPart(typeof(OrdersController).Assembly);
        var databaseName = Guid.NewGuid().ToString();
        builder.Services.AddDbContext<OrdersDbContext>(options => options.UseInMemoryDatabase(databaseName));
        builder.Services.AddScoped<IOrderRepository, OrderRepository>();
        builder.Services.AddScoped<CreateOrderHandler>();
        builder.Services.AddScoped<GetOrdersHandler>();
        builder.Services.AddScoped<GetOrderByIdHandler>();

        app = builder.Build();
        app.MapControllers();
        await app.StartAsync();
        client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
    }

    [Fact]
    public async Task Create_Returns201WithIdAndLocationThatRetrievesCreatedOrder()
    {
        using var response = await client.PostAsJsonAsync("/api/orders", new
        {
            clientName = "Sample Client",
            items = new[]
            {
                new { sku = "SKU-1", quantity = 2 },
                new { sku = "SKU-2", quantity = 1 }
            }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Single(body.EnumerateObject());
        var id = body.GetProperty("id").GetGuid();
        Assert.NotEqual(Guid.Empty, id);
        var location = response.Headers.Location;
        Assert.NotNull(location);
        Assert.Equal($"/api/orders/{id}", location.AbsolutePath, ignoreCase: true);

        using var getResponse = await client.GetAsync(location);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var order = await getResponse.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.NotNull(order);
        Assert.Equal(id, order.Id);
        Assert.Equal("Sample Client", order.ClientName);
        Assert.Equal("Pending", order.Status);
        Assert.Equal(2, order.Items.Count);
        Assert.Contains(order.Items, item => item.Sku == "SKU-1" && item.Quantity == 2);
        Assert.Contains(order.Items, item => item.Sku == "SKU-2" && item.Quantity == 1);

        using var listResponse = await client.GetAsync("/api/orders");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var orders = await listResponse.Content.ReadFromJsonAsync<OrderResponse[]>();
        Assert.NotNull(orders);
        var listedOrder = Assert.Single(orders);
        Assert.Equal(order.Id, listedOrder.Id);
        Assert.Equal(order.ClientName, listedOrder.ClientName);
        Assert.Equal(order.CreatedAt, listedOrder.CreatedAt);
        Assert.Equal(order.Status, listedOrder.Status);
        Assert.Equal(order.Items, listedOrder.Items);
    }

    [Theory]
    [InlineData("11111111-1111-1111-1111-111111111111")]
    [InlineData("not-a-guid")]
    public async Task GetById_Returns404ForMissingOrderOrInvalidRoute(string id)
    {
        using var response = await client.GetAsync($"/api/orders/{id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_InvalidRequestStillReturns400WithoutCreatingOrder()
    {
        using var response = await client.PostAsJsonAsync("/api/orders", new
        {
            clientName = "Sample Client",
            items = Array.Empty<object>()
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(response.Headers.Location);
        var orders = await client.GetFromJsonAsync<OrderResponse[]>("/api/orders");
        Assert.NotNull(orders);
        Assert.Empty(orders);
    }

    public async Task DisposeAsync()
    {
        client.Dispose();
        await app.DisposeAsync();
    }
}
