using System;
using System.Collections.Generic;
using System.Text;

namespace Orders.Application.Events
{
    public sealed record OrderCreated(Guid OrderId, List<OrderCreatedItem> Items);

    public sealed record OrderCreatedItem(string Sku, int Quantity);
}
