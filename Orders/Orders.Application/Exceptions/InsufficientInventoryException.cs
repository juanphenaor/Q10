using System;
using System.Collections.Generic;
using System.Text;

namespace Orders.Application.Exceptions
{
    public class InsufficientInventoryException : Exception
    {
        public class InventoryException(string code, string message) : Exception(message)
        {
            public string Code { get; } = code;
        }
    }
}
