using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.Events
{
    public sealed class StockReservationResult
    {
        public bool IsAccepted { get; init; }

        public StockReserved? Reservation { get; init; }

        public StockRejected? Rejection { get; init; }
    }
}
