using System;
using System.Collections.Generic;
using System.Text;

namespace Orders.Application.DTOs
{
    public class CreateOrderRequest
    {
        public Guid CustomerId { get; set; }
        public List<OrderItemRequest> Items { get; set; } = [];
    }
}
