using Inventory.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Context
{
    public class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
    {
        public DbSet<Product> Products => Set<Product>();
    }
}