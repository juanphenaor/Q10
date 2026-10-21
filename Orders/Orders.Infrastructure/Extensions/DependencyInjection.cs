using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orders.Application.Contracts;
using Orders.Infrastructure.Context;
using Orders.Infrastructure.Repositories;
using Orders.Infrastructure.Services;

namespace Orders.Infrastructure.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("OrdersDatabase")
               ?? throw new InvalidOperationException("La cadena de conexión OrdersDatabase no está configurada.");

            services.AddDbContext<OrdersDbContext>(options => options.UseNpgsql(connectionString));

            services.AddHostedService<OutboxPublisher>();
            services.AddScoped<IOrderRepository, OrderRepository>();

            return services;
        }
    }
}
