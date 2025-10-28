using Infinity.Toolkit.AspNetCore;

namespace Infinity.Toolkit.Results.Tests;

public class ResultExtensionsTests
{
    [Fact]
    public void Value_WithSuccessResult_ReturnsValue()
    {
        // Arrange
        var expectedValue = "test value";
        var result = Result.Success(expectedValue);

        // Act
        var actualValue = result.Value();

        // Assert
        Assert.Equal(expectedValue, actualValue);
    }

    [Fact]
    public void Value_WithFailureResult_Throws_InvalidOperationException()
    {
        // Arrange
        var result = Result.Failure<string>("Error occurred");

        // Act
        // Assert
        Should.Throw<InvalidOperationException>(() => result.Value())
            .Message.ShouldBe("You can't access .Value when .Succeeded is false");
    }

    [Fact]
    public void Match_WithSuccessResult_CallsOnSuccess()
    {
        // Arrange
        var result = Result.Success();
        var expectedValue = "success";

        // Act
        var actualValue = result.Match(
            onSuccess: () => expectedValue,
            onFailure: _ => "failure"
        );

        // Assert
        Assert.Equal(expectedValue, actualValue);
    }

    [Fact]
    public void Match_WithFailureResult_CallsOnFailure()
    {
        // Arrange
        var error = new Error("TEST001", "Test error");
        var result = Result.Failure(error);
        var expectedValue = "failure";

        // Act
        var actualValue = result.Match(
            onSuccess: () => "success",
            onFailure: errors => expectedValue
        );

        // Assert
        Assert.Equal(expectedValue, actualValue);
    }

    [Fact]
    public void ToResult_WithValue_ReturnsSuccessResult()
    {
        // Arrange
        var value = "test value";

        // Act
        var result = value.ToResult();

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(value, result.Value());
    }

    [Fact]
    public void ToProblemDetails_WithFailureResult_ReturnsProblemDetails()
    {
        // Arrange
        var error = new Error("TEST001", "Test error details");
        var result = Result.Failure(error);

        // Act
        var problemDetails = result.ToProblemDetails();

        // Assert
        problemDetails.Detail.ShouldNotBeNullOrEmpty();
        problemDetails.Detail.ShouldBe("Test error details");
        problemDetails.Status.ShouldBe(400);
        problemDetails.Extensions["errors"].ShouldNotBeNull();

        var errors = problemDetails.Extensions["errors"] as IReadOnlyCollection<Error>;
        errors.ShouldNotBeNull();
        errors.Count.ShouldBe(1);
        errors.First().Code.ShouldBe("TEST001");
    }

    [Fact]
    public void ToProblemDetails_WithSuccessResult_ThrowsInvalidOperationException()
    {
        // Arrange
        var result = Result.Success();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => result.ToProblemDetails());
    }
}
