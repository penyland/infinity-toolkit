using System.ComponentModel.DataAnnotations;

namespace Infinity.Toolkit.Tests;

public class RequiredIfAttributeTests
{
    private class TestModel
    {
        public string? Property1 { get; set; }

        [RequiredIf(nameof(Property1), "property1")]
        public string? Property2 { get; set; }
    }

    [Fact]
    public void Should_Be_Valid_When_Property1_Is_Value()
    {
        // Arrange
        var model = new TestModel { Property1 = "property1", Property2 = "property2" };
        var validationContext = new ValidationContext(model);
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(model, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Be_Valid_When_Property1_Is_Null_And_Property2_Is_Null()
    {
        // Arrange
        var model = new TestModel { Property1 = null, Property2 = null };
        var validationContext = new ValidationContext(model);
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(model, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Be_Valid_When_Property1_Is_Null_And_Property2_Is_Empty()
    {
        // Arrange
        var model = new TestModel { Property1 = null, Property2 = string.Empty };
        var validationContext = new ValidationContext(model);
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(model, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Be_Invalid_When_Property2_Is_Null()
    {
        // Arrange
        var model = new TestModel { Property1 = "property1", Property2 = null };
        var validationContext = new ValidationContext(model);
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(model, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeFalse();
    }
}
