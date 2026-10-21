using System;
using System.Collections.Generic;
using System.Text;

namespace Orders.Domain.Enumerations
{
    public enum OrderStatus
    {
        Pending,
        Confirmed,
        Rejected,
        Cancelled
    }
}
