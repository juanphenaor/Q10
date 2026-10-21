using System;
using System.Collections.Generic;
using System.Text;

namespace Orders.Application.DTOs
{
    public class InventoryErrorResponse
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
