using System.ComponentModel.DataAnnotations;

namespace Inventory.Application.DTOs
{
    public class AvailabilityRequest
    {
        [Required]
        [MinLength(1)]
        public List<AvailabilityItemRequest> Items { get; set; } = [];
    }

    public class AvailabilityItemRequest
    {
        [Required]
        [RegularExpression(@".*\S.*")]
        public string Sku { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }
}