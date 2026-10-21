using System.ComponentModel.DataAnnotations;

namespace Orders.Application.DTOs
{
    public class CreateOrderRequest
    {
        [Required]
        public Guid CustomerId { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "La orden debe contener al menos un producto.")]
        public List<OrderItemRequest> Items { get; set; } = [];
    }
}
