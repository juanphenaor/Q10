using System;
using System.Collections.Generic;
using System.Text;

namespace Orders.Application.Events
{
    public sealed record OrderCreated
    {
        public Guid OrderId { get; init; }

        public List<OrderCreatedItem> Items { get; init; } = [];
    }

    public sealed record OrderCreatedItem
    {
        public string Sku { get; init; } = string.Empty;

        public int Quantity { get; init; }
    }
}
