using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orders.Application.Contracts;
using Orders.Infrastructure.Clients;
using Orders.Infrastructure.Context;
using Orders.Infrastructure.Repositories;

namespace Orders.Infrastructure.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("OrdersDatabase")
               ?? throw new InvalidOperationException("La cadena de conexión OrdersDatabase no está configurada.");

            services.AddDbContext<OrdersDbContext>(options => options.UseNpgsql(connectionString));

            services.AddScoped<IOrderRepository, OrderRepository>();

            services.AddHttpClient<IInventoryClient, InventoryClient>(client =>
            {
                client.BaseAddress = new Uri(configuration["Inventory:BaseUrl"] ?? throw new InvalidOperationException("La URL base de Inventory no está configurada."));
            });

            return services;
        }
    }
}
