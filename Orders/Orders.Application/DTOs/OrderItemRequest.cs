using System;
using System.Collections.Generic;
using System.Text;

namespace Orders.Application.DTOs
{
    public class OrderItemRequest
    {
        public string Sku { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}
