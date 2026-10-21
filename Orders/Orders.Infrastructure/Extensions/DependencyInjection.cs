using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
            services.AddHostedService<StockResultConsumer>();
            services.AddScoped<IOrderRepository, OrderRepository>();

            services.AddOptions<InventoryClientOptions>()
                .Bind(configuration.GetSection(InventoryClientOptions.SectionName))
                .Validate(options =>
                    Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri) &&
                    (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps),
                    "Inventory:BaseUrl must be an absolute HTTP or HTTPS URL.")
                .ValidateOnStart();
            services.AddHttpClient<IInventoryClient, InventoryClient>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<InventoryClientOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
                client.Timeout = InventoryClientOptions.RequestTimeout;
            });

            return services;
        }
    }
}
