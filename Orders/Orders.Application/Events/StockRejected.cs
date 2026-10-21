using System;
using System.Collections.Generic;
using System.Text;

namespace Orders.Application.Events
{
    public sealed record StockRejected(Guid OrderId, string Code, string Reason);
}
