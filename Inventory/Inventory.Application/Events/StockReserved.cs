using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.Events
{
    public sealed record StockReserved
    {
        public Guid OrderId { get; init; }

        public List<StockReservedItem> Items { get; init; } = [];
    }

    public sealed record StockReservedItem
    {
        public string Sku { get; init; } = string.Empty;

        public int Quantity { get; init; }

        public decimal UnitPrice { get; init; }
    }
}
