using Orders.Domain.Enumerations;

namespace Orders.Domain.Models
{
    public class Order
    {
        public Guid Id { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public OrderStatus Status { get; set; }
        public List<OrderItem> Items { get; set; } = [];
    }
}
