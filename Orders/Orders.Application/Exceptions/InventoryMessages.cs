namespace Orders.Application.Exceptions;

public static class InventoryMessages
{
    public const string InvalidSkusCode = "INVALID_SKUS";
    public const string InvalidSkus = "La petición contiene SKU no válidos";
    public const string UnavailableCode = "INVENTORY_UNAVAILABLE";
    public const string Unavailable = "No fue posible validar los SKU con Inventory. Inténtalo de nuevo.";
    public const string ValidationFailed = "Inventory SKU validation failed.";
}
