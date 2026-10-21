using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Domain.Models
{
    public sealed class ProcessedOrder
    {
        public Guid OrderId { get; set; }
        public bool IsAccepted { get; set; }
        public string Result { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; }
    }
}
