using System.ComponentModel.DataAnnotations;

namespace Orders.Application.DTOs
{
    public class OrderItemRequest
    {
        [Required]
        [RegularExpression(@".*\S.*")]
        public string Sku { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }
}
