using Orders.Domain.Enumerations;
using System;
using System.Collections.Generic;
using System.Text;

namespace Orders.Domain.Models
{
    public class Order
    {
        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        public DateTime CreatedAt { get; set; }
        public OrderStatus Status { get; set; }
        public List<OrderItem> Items { get; set; } = [];
    }
}
