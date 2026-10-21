using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orders.Api.Controllers;
using Orders.Api.Middlewares;
using Orders.Application.Contracts;
using Orders.Application.Handlers;
using Orders.Infrastructure.Context;
using Orders.Infrastructure.Repositories;
using Xunit;

namespace Backend.Tests;

public sealed class OrdersEndpointTests : IAsyncLifetime
{
    private WebApplication app = null!;
    private HttpClient client = null!;
    private readonly StubInventoryClient inventoryClient = new();

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddControllers().AddApplicationPart(typeof(OrdersController).Assembly);
        var databaseName = Guid.NewGuid().ToString();
        builder.Services.AddDbContext<OrdersDbContext>(options => options.UseInMemoryDatabase(databaseName));
        builder.Services.AddScoped<IOrderRepository, OrderRepository>();
        builder.Services.AddSingleton<IInventoryClient>(inventoryClient);
        builder.Services.AddScoped<CreateOrderHandler>();
        builder.Services.AddScoped<GetOrdersHandler>();
        builder.Services.AddScoped<GetOrderByIdHandler>();

        app = builder.Build();
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.MapControllers();
        await app.StartAsync();
        client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
    }

    [Fact]
    public async Task Create_MissingSkuReturns409WithoutOrderOrOutbox()
    {
        inventoryClient.Result = false;

        using var response = await PostOrderAsync();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("INVALID_SKUS", body.GetProperty("code").GetString());
        Assert.Equal("La petición contiene SKU no válidos", body.GetProperty("message").GetString());
        Assert.Null(response.Headers.Location);
        await AssertNothingPersistedAsync();
    }

    private Task<HttpResponseMessage> PostOrderAsync() => client.PostAsJsonAsync("/api/orders", new
    {
        clientName = "Sample Client",
        items = new[] { new { sku = "SKU-1", quantity = 1 } }
    });

    private async Task AssertNothingPersistedAsync()
    {
        await using var scope = app.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        Assert.Empty(await context.Orders.ToListAsync());
        Assert.Empty(await context.OrderItems.ToListAsync());
        Assert.Empty(await context.OutboxMessages.ToListAsync());
    }

    public async Task DisposeAsync()
    {
        client.Dispose();
        await app.DisposeAsync();
    }
}
