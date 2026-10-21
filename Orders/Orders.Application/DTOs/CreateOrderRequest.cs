using System.ComponentModel.DataAnnotations;

namespace Orders.Application.DTOs
{
    public class CreateOrderRequest
    {
        [Required]
        [StringLength(200)]
        [RegularExpression(@".*\S.*")]
        public string ClientName { get; set; } = string.Empty;

        [Required]
        [MinLength(1, ErrorMessage = "La orden debe contener al menos un producto.")]
        public List<OrderItemRequest> Items { get; set; } = [];
    }
}
