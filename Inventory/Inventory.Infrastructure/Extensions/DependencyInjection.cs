using Inventory.Application.Contracts;
using Inventory.Infrastructure.Context;
using Inventory.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Infrastructure.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("InventoryDatabase")
                ?? throw new InvalidOperationException("La cadena de conexión InventoryDatabase no está configurada.");

            services.AddDbContext<InventoryDbContext>(options => options.UseNpgsql(connectionString));
            services.AddScoped<IProductRepository, ProductRepository>();

            return services;
        }
    }
}