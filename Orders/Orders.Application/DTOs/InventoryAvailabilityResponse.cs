using System;
using System.Collections.Generic;
using System.Text;

namespace Orders.Application.DTOs
{
    public class InventoryAvailabilityResponse
    {
        public List<InventoryItemResponse> Items { get; set; } = [];
    }

    public class InventoryItemResponse
    {
        public string Sku { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
    }
}
