using System;
using System.Collections.Generic;
using System.Text;

namespace Orders.Application.Events
{
    public sealed record StockReserved(Guid OrderId, List<StockReservedItem> Items);

    public sealed record StockReservedItem(string Sku, int Quantity, decimal UnitPrice);
}
