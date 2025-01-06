using System.ComponentModel.DataAnnotations;

namespace Infinity.Toolkit;

/// <summary>
/// Specifies that a data field value is required when another property is set to a specified value.
/// </summary>
public sealed class RequiredIfAttribute(string propertyName, object? isValue) : ValidationAttribute
{
    private readonly string propertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
    private readonly object? isValue = isValue;

    public override string FormatErrorMessage(string name)
    {
        var errorMessage = $"Property {name} is required when {propertyName} is {isValue}";
        return ErrorMessage ?? errorMessage;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        ArgumentNullException.ThrowIfNull(validationContext, nameof(validationContext));
        var property = validationContext.ObjectType.GetProperty(propertyName);

        if (property is null)
        {
            return new ValidationResult($"Property {propertyName} not found.");
        }

        var actualValue = property.GetValue(validationContext.ObjectInstance);
        if (actualValue == null && isValue != null)
        {
            return ValidationResult.Success;
        }

        if (actualValue == null || actualValue.Equals(isValue))
        {
            return value == null ? new ValidationResult(ErrorMessage) : ValidationResult.Success;
        }

        return ValidationResult.Success;
    }
}
