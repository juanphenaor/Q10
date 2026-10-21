namespace Inventory.Domain.Models
{
    public class Product
    {
        public Guid Id { get; set; }
        public string Sku { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int AvailableQuantity { get; set; }
    }
}