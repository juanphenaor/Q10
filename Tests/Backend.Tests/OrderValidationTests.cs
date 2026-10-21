using System.ComponentModel.DataAnnotations;
using Orders.Application.DTOs;
using Xunit;

namespace Backend.Tests;

public sealed class OrderValidationTests
{
    [Fact]
    public void CreateOrderRequest_RejectsInvalidClientItemsSkusAndQuantities()
    {
        var emptyOrder = Validate(new CreateOrderRequest
        {
            ClientName = "María Pérez",
            Items = []
        });
        var blankName = Validate(new CreateOrderRequest
        {
            ClientName = "   ",
            Items = [new OrderItemRequest { Sku = "SKU-1", Quantity = 1 }]
        });
        var tooLongName = Validate(new CreateOrderRequest
        {
            ClientName = new string('a', 201),
            Items = [new OrderItemRequest { Sku = "SKU-1", Quantity = 1 }]
        });
        var blankSku = Validate(new OrderItemRequest { Sku = "   ", Quantity = 1 });
        var zeroQuantity = Validate(new OrderItemRequest { Sku = "SKU-1", Quantity = 0 });
        var excessiveQuantity = Validate(new OrderItemRequest { Sku = "SKU-1", Quantity = 101 });

        Assert.Contains(emptyOrder, result =>
            result.MemberNames.Contains(nameof(CreateOrderRequest.Items)));
        Assert.Contains(blankName, result =>
            result.MemberNames.Contains(nameof(CreateOrderRequest.ClientName)));
        Assert.Contains(tooLongName, result =>
            result.MemberNames.Contains(nameof(CreateOrderRequest.ClientName)));
        Assert.Contains(blankSku, result =>
            result.MemberNames.Contains(nameof(OrderItemRequest.Sku)));
        Assert.Contains(zeroQuantity, result =>
            result.MemberNames.Contains(nameof(OrderItemRequest.Quantity)));
        Assert.Contains(excessiveQuantity, result =>
            result.MemberNames.Contains(nameof(OrderItemRequest.Quantity)));
    }

    private static List<ValidationResult> Validate(object value)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(
            value,
            new ValidationContext(value),
            results,
            validateAllProperties: true);
        return results;
    }
}
