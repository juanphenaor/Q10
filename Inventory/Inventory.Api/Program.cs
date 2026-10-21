using Inventory.Api.Middlewares;
using Inventory.Application.Handlers;
using Inventory.Domain.Models;
using Inventory.Infrastructure.Context;
using Inventory.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<ValidateSkusHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseMiddleware<ExceptionHandlingMiddleware>();

await using (var scope = app.Services.CreateAsyncScope())
{
    var context = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

    await context.Database.MigrateAsync();
}

await SeedDevelopmentProductsAsync(app);

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();

static async Task SeedDevelopmentProductsAsync(WebApplication app)
{
    await using var scope = app.Services.CreateAsyncScope();
    var context = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    var seedProducts = new[]
    {
        new Product
        {
            Id = Guid.NewGuid(),
            Sku = "SKU-001",
            UnitPrice = 25000m,
            AvailableQuantity = 100
        },
        new Product
        {
            Id = Guid.NewGuid(),
            Sku = "SKU-002",
            UnitPrice = 18000m,
            AvailableQuantity = 100
        },
        new Product
        {
            Id = Guid.NewGuid(),
            Sku = "SKU-003",
            UnitPrice = 22000m,
            AvailableQuantity = 100
        }
    };
    var seedSkus = seedProducts.Select(product => product.Sku).ToArray();
    var existingSkus = await context.Products
        .Where(product => seedSkus.Contains(product.Sku))
        .Select(product => product.Sku)
        .ToListAsync();
    var existingSkuSet = existingSkus.ToHashSet(StringComparer.Ordinal);
    var missingProducts = seedProducts
        .Where(product => !existingSkuSet.Contains(product.Sku))
        .ToArray();

    if (missingProducts.Length > 0)
    {
        context.Products.AddRange(missingProducts);
        await context.SaveChangesAsync();
    }
}
