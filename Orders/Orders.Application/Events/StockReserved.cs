using System;
using System.Collections.Generic;
using System.Text;

namespace Orders.Application.Events
{
    public sealed record StockReserved
    {
        public Guid OrderId { get; init; }
    }
}
