using System.ComponentModel.DataAnnotations;
using Orders.Application.DTOs;
using Xunit;

namespace Backend.Tests;

public sealed class OrderValidationTests
{
    [Fact]
    public void CreateOrderRequest_RequiresAtLeastOneItem()
    {
        var request = new CreateOrderRequest
        {
            ClientName = "María Pérez",
            Items = []
        };

        var results = Validate(request);

        Assert.Contains(results, result =>
            result.MemberNames.Contains(nameof(CreateOrderRequest.Items)));
    }

    [Fact]
    public void CreateOrderRequest_RejectsBlankOrTooLongClientName()
    {
        var blankNameRequest = new CreateOrderRequest
        {
            ClientName = "   ",
            Items = [new OrderItemRequest { Sku = "SKU-1", Quantity = 1 }]
        };
        var tooLongNameRequest = new CreateOrderRequest
        {
            ClientName = new string('a', 201),
            Items = [new OrderItemRequest { Sku = "SKU-1", Quantity = 1 }]
        };

        var blankNameResults = Validate(blankNameRequest);
        var tooLongNameResults = Validate(tooLongNameRequest);

        Assert.Contains(blankNameResults, result =>
            result.MemberNames.Contains(nameof(CreateOrderRequest.ClientName)));
        Assert.Contains(tooLongNameResults, result =>
            result.MemberNames.Contains(nameof(CreateOrderRequest.ClientName)));
    }

    [Fact]
    public void OrderItemRequest_RejectsBlankSkuAndNonPositiveQuantity()
    {
        var item = new OrderItemRequest
        {
            Sku = "   ",
            Quantity = 0
        };

        var results = Validate(item);

        Assert.Contains(results, result =>
            result.MemberNames.Contains(nameof(OrderItemRequest.Sku)));
        Assert.Contains(results, result =>
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
