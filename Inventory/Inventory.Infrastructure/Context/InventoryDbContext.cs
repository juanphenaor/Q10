using Inventory.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Context
{
    public class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
    {
        public DbSet<Product> Products => Set<Product>();

        public DbSet<ProcessedOrder> ProcessedOrders => Set<ProcessedOrder>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ProcessedOrder>(entity =>
            {
                entity.HasKey(order => order.OrderId);
            });
        }
    }
}