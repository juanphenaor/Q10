namespace Inventory.Application.Exceptions
{
    public class InventoryException(string code, string message, int statusCode) : Exception(message)
    {
        public string Code { get; } = code;
        public int StatusCode { get; } = statusCode;
    }
}