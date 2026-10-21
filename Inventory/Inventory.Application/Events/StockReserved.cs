using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.Events
{
    public sealed record StockReserved
    {
        public Guid OrderId { get; init; }
    }
}
