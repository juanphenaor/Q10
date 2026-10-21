using System.ComponentModel.DataAnnotations;

namespace Orders.Application.DTOs
{
    public class OrderItemRequest
    {
        public const int MaxQuantity = 100;

        [Required]
        [RegularExpression(@".*\S.*")]
        public string Sku { get; set; } = string.Empty;

        [Range(1, MaxQuantity)]
        public int Quantity { get; set; }
    }
}
