using System;
using System.Collections.Generic;
using System.Text;

namespace Orders.Domain.Models
{
    public class OrderItem
    {
        public Guid Id { get; set; }
        public string Sku { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        public Guid OrderId { get; set; }
        public Order Order { get; set; } = null!;
    }
}
