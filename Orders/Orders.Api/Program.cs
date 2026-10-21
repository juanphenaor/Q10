using Microsoft.EntityFrameworkCore;
using Orders.Api.Hubs;
using Orders.Api.Middlewares;
using Orders.Api.Services;
using Orders.Application.Contracts;
using Orders.Application.Handlers;
using Orders.Infrastructure.Context;
using Orders.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

builder.Services.AddScoped<IOrderNotifier, NotificationsService>();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<CreateOrderHandler>();
builder.Services.AddScoped<GetOrdersHandler>();
builder.Services.AddScoped<GetOrderByIdHandler>();

var app = builder.Build();

app.UseCors("Frontend");
app.UseMiddleware<ExceptionHandlingMiddleware>();

await using (var scope = app.Services.CreateAsyncScope())
{
    var context = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();

    await context.Database.MigrateAsync();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapHub<OrdersHub>("/hubs/orders");

app.Run();
