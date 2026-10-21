using System.ComponentModel.DataAnnotations;

namespace Inventory.Application.DTOs;

/// <summary>Specifies the product SKUs whose existence must be checked.</summary>
public sealed record ValidateSkusRequest : IValidatableObject
{
    [Required]
    [MinLength(1)]
    public string[] Skus { get; init; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Skus is not null && Skus.Any(string.IsNullOrWhiteSpace))
        {
            yield return new ValidationResult(
                "Each SKU must contain a nonblank value.",
                [nameof(Skus)]);
        }
    }
}
