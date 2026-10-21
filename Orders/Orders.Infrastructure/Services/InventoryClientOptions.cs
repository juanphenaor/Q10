namespace Orders.Infrastructure.Services;

public sealed class InventoryClientOptions
{
    public const string SectionName = "Inventory";
    public static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(5);

    public string BaseUrl { get; set; } = string.Empty;
}
