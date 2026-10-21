using System;
using System.Collections.Generic;
using System.Text;

namespace Orders.Application.Events
{
    public sealed record StockRejected
    {
        public Guid OrderId { get; init; }

        public string Code { get; init; } = string.Empty;

        public string Reason { get; init; } = string.Empty;
    }
}
