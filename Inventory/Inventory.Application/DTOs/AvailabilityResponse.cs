namespace Inventory.Application.DTOs
{
    public class AvailabilityResponse
    {
        public List<AvailabilityItemResponse> Items { get; set; } = [];
    }

    public class AvailabilityItemResponse
    {
        public string Sku { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
    }
}