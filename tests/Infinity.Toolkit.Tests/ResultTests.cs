namespace Infinity.Toolkit.Results.Tests;

public class ResultTests
{
    [Fact]
    public void Result_Success_Should_Be_Successful()
    {
        var result = Result.Success();
        result.Succeeded.ShouldBeTrue();
        result.Failed.ShouldBeFalse();
        result.Errors.ShouldNotBeNull();
        result.Errors.Count.ShouldBe(0);        
    }

    [Fact]
    public void Result_Failure_Should_Not_Be_Successful()
    {
        var result = Result.Failure("An error occurred");
        result.Succeeded.ShouldBeFalse();
        result.Failed.ShouldBeTrue();
        result.Errors.ShouldNotBeNull();
        result.Errors.First().Message.ShouldBe(string.Empty);
        result.Errors.First().Details.ShouldBe("An error occurred");
    }

    [Fact]
    public void Result_With_Value_Should_Return_Value_When_Successful()
    {
        var value = "Test Value";
        var result = Result<string>.Success(value);
        result.Succeeded.ShouldBeTrue();
        result.Value.ShouldBe(value);
    }

    [Fact]
    public void Result_With_Value_Should_Throw_When_Failed()
    {
        var result = Result<string>.Failure("An error occurred");
        result.Succeeded.ShouldBeFalse();

        Should.Throw<InvalidOperationException>(() => _ = result.Value)
            .Message.ShouldBe("You can't access .Value when .Succeeded is false");
    }

    [Fact]
    public void Result_Match_Should_Execute_OnSuccess_When_Successful()
    {
        var value = "Test Value";
        var result = Result<string>.Success(value);

        var matchedValue = result.Match(
            onSuccess: () => "Success",
            onFailure: errors => "Failure"
        );

        matchedValue.ShouldBe("Success");
    }

    [Fact]
    public void Result_Match_Should_Execute_OnFailure_When_Failed()
    {
        var result = Result.Failure("An error occurred");

        var matchedValue = result.Match(
            onSuccess: () => "Success",
            onFailure: errors => "Failure"
        );

        matchedValue.ShouldBe("Failure");
    }

    [Fact]
    public void Result_ToResult_Should_Return_SuccessResult_With_Value()
    {
        var value = "Test Value";
        var result = value.ToResult();

        result.Succeeded.ShouldBeTrue();
        result.Value.ShouldBe(value);
    }

    [Fact]
    public void Result_Value_Should_Return_Value_When_Successful()
    {
        var value = "Test Value";
        var result = Result<string>.Success(value);

        var extractedValue = result.Value();

        extractedValue.ShouldBe(value);
    }

    [Fact]
    public void Result_Value_Should_Throw_InvalidOperationException_When_Failed()
    {
        var result = Result<string>.Failure("An error occurred");

        Should.Throw<InvalidOperationException>(() => _ = result.Value())
            .Message.ShouldBe("You can't access .Value when .Succeeded is false");
    }

    [Fact]
    public void Result_Implicit_Conversion_From_Value_Should_Work()
    {
        var value = "Test Value";
        Result<string> result = value;

        result.Succeeded.ShouldBeTrue();
        result.Value.ShouldBe(value);
    }

    [Fact]
    public void Result_Implicit_Conversion_From_Error_Should_Work()
    {
        var error = new Error("TestError", "An error occurred");
        Result<string> result = error;

        result.Succeeded.ShouldBeFalse();
        result.Errors.Count.ShouldBe(1);
        result.Errors.First().Message.ShouldBe("TestError");
        result.Errors.First().Details.ShouldBe("An error occurred");
    }

    [Fact]
    public void Result_Implicit_Conversion_To_Value_Should_Work()
    {
        var value = "Test Value";
        var result = Result<string>.Success(value);

        string extractedValue = result;

        extractedValue.ShouldBe(value);
    }

    [Fact]
    public void Result_Implicit_Conversion_To_Error_Should_Work()
    {
        var error = new Error("TestError", "An error occurred");
        var result = Result.Failure(error);

        Error[] extractedErrors = result;

        extractedErrors.First().Message.ShouldBe("TestError");
        extractedErrors.First().Details.ShouldBe("An error occurred");
    }

    [Fact]
    public void Result_Implicit_Conversion_From_Success_Should_Work()
    {
        var result = Result<string>.Success("Test Value");
        Error[] extractedErrors = result;

        extractedErrors.First().ShouldBe(Error.None);
    }

    [Fact]
    public void Result_Implicit_Conversion_From_Result_Should_Work()
    {
        var result = Result<string>.Success("Test Value");
        var convertedResult = result;

        convertedResult.Succeeded.ShouldBeTrue();
        convertedResult.Value.ShouldBe("Test Value");
    }

    [Fact]
    public void Result_Implicit_Conversion_To_Result_Should_Work()
    {
        var value = "Test Value";
        Result<string> result = value;

        var convertedResult = result;

        convertedResult.Succeeded.ShouldBeTrue();
        convertedResult.Value.ShouldBe(value);
    }

    [Fact]
    public void IsSuccess_Should_Return_True_When_Successful()
    {
        var result = Result<string>.Success("Test Value");
        (result is Success).ShouldBeTrue();
    }

    [Fact]
    public void IsFailure_Should_Return_True_When_Failed()
    {
        var result = Result<string>.Failure("An error occurred");
        (result is Failure).ShouldBeTrue();
    }

    [Fact]
    public void Result_With_Generic_Value_Should_Be_Successful()
    {
        var result = Result<TestResponse>.Success(new TestResponse("Hello, World!"));
        result.Succeeded.ShouldBeTrue();
        result.Value.Message.ShouldBe("Hello, World!");
    }

    [Fact]
    public void Result_With_Generic_Value_Should_Be_Failure_When_Error_Occurs()
    {
        var result = Result<TestResponse>.Failure("An error occurred");
        result.Succeeded.ShouldBeFalse();
        result.Errors.Count.ShouldBe(1);
        result.Errors.First().Details.ShouldBe("An error occurred");
    }

    [Fact]
    public void Result_With_Generic_Value_Should_Return_Value_When_Successful()
    {
        var response = new TestResponse("Test Response");
        var result = Result<TestResponse>.Success(response);

        result.Succeeded.ShouldBeTrue();
        result.Value.Message.ShouldBe("Test Response");
    }

    [Fact]
    public void Result_With_Generic_Value_Should_Throw_When_Failed()
    {
        var result = Result<TestResponse>.Failure("An error occurred");
        result.Succeeded.ShouldBeFalse();

        Should.Throw<InvalidOperationException>(() => _ = result.Value)
            .Message.ShouldBe("You can't access .Value when .Succeeded is false");
    }    

    [Fact]
    public void Result_With_Generic_Value_Should_Implicitly_Convert_From_Value()
    {
        TestResponse response = new("Test Response");
        Result<TestResponse> result = response;

        result.Succeeded.ShouldBeTrue();
        result.Value.Message.ShouldBe("Test Response");
    }

    [Fact]
    public void Result_With_Generic_Value_Should_Implicitly_Convert_From_Error()
    {
        var error = new Error("TestError", "An error occurred");
        Result<TestResponse> result = error;

        result.Succeeded.ShouldBeFalse();
        result.Errors.Count.ShouldBe(1);
        result.Errors.First().Message.ShouldBe("TestError");
        result.Errors.First().Details.ShouldBe("An error occurred");
    }

    [Fact]
    public void Generic_Failure_With_Exception_Should_Be_Failure()
    {
        var exception = new InvalidOperationException("Test Exception");
        var result = Result<TestResponse>.Failure(exception);

        result.Succeeded.ShouldBeFalse();
        result.Errors.Count.ShouldBe(1);

        var exceptionError = result.Errors.First() as ExceptionError;
        exceptionError.ShouldNotBeNull();
        exceptionError.Exception.ShouldBe(exception);
        exceptionError.Message.ShouldBe(exception.GetType().Name);
        exceptionError.Details.ShouldBe("Test Exception");
    }

    [Fact]
    public void Generic_Failure_With_Exception_Should_Throw_When_Accessing_Value()
    {
        var exception = new InvalidOperationException("Test Exception");
        var result = Result<TestResponse>.Failure(exception);

        result.Succeeded.ShouldBeFalse();

        Should.Throw<InvalidOperationException>(() => _ = result.Value)
            .Message.ShouldBe("You can't access .Value when .Succeeded is false");
    }

    [Fact]
    public void Generic_Result_Should_Implicitly_Convert_From_Exception()
    {
        var exception = new InvalidOperationException("Test Exception");
        Result<TestResponse> result = exception;

        result.Succeeded.ShouldBeFalse();
        result.Errors.Count.ShouldBe(1);
        var exceptionError = result.Errors.First() as ExceptionError;
        exceptionError.ShouldNotBeNull();
        exceptionError.Exception.ShouldBe(exception);
        exceptionError.Message.ShouldBe(exception.GetType().Name);
        exceptionError.Details.ShouldBe("Test Exception");
    }

    [Fact]
    public void Generic_Failure_With_Error_Should_Be_Failure()
    {
        var error = new Error("TestError", "An error occurred");
        var result = Result<TestResponse>.Failure(error);

        result.Succeeded.ShouldBeFalse();
        result.Errors.Count.ShouldBe(1);
        result.Errors.First().Message.ShouldBe("TestError");
        result.Errors.First().Details.ShouldBe("An error occurred");
    }

    [Fact]
    public void Generic_Failure_With_Error_Should_Throw_When_Accessing_Value()
    {
        var error = new Error("TestError", "An error occurred");
        var result = Result<TestResponse>.Failure(error);

        result.Succeeded.ShouldBeFalse();

        Should.Throw<InvalidOperationException>(() => _ = result.Value)
            .Message.ShouldBe("You can't access .Value when .Succeeded is false");
    }

    [Fact]
    public void Generic_Failure_With_Error_Should_Implicitly_Convert_From_Error()
    {
        var error = new Error("TestError", "An error occurred");
        Result<TestResponse> result = error;

        result.Succeeded.ShouldBeFalse();
        result.Errors.Count.ShouldBe(1);
        result.Errors.First().Message.ShouldBe("TestError");
        result.Errors.First().Details.ShouldBe("An error occurred");
    }

    [Fact]
    public void Generic_Failure_With_Message_And_Errors_Should_Be_Failure()
    {
        var errors = new List<Error> { new("TestError", "An error occurred") };
        var result = Result<TestResponse>.Failure("Failure message", errors);

        result.Succeeded.ShouldBeFalse();
        result.Errors.Count.ShouldBe(1);
        result.Errors.First().Message.ShouldBe("TestError");
        result.Errors.First().Details.ShouldBe("An error occurred");
    }

    [Fact]
    public void Generic_Failure_With_Message_And_Errors_Should_Throw_When_Accessing_Value()
    {
        var errors = new List<Error> { new("TestError", "An error occurred") };
        var result = Result<TestResponse>.Failure("Failure message", errors);

        result.Succeeded.ShouldBeFalse();

        Should.Throw<InvalidOperationException>(() => _ = result.Value)
            .Message.ShouldBe("You can't access .Value when .Succeeded is false");
    }

    [Fact]
    public void Generic_Failure_With_Message_And_Multiple_Errors_Should_Be_Failure()
    {
        var errors = new List<Error>
        {
            new("TestError1", "First error occurred"),
            new("TestError2", "Second error occurred")
        };

        var result = Result<TestResponse>.Failure("Failure message", errors);

        result.Succeeded.ShouldBeFalse();
        result.Errors.Count.ShouldBe(2);
        result.Errors.ElementAt(0).Message.ShouldBe("TestError1");
        result.Errors.ElementAt(0).Details.ShouldBe("First error occurred");
        result.Errors.ElementAt(1).Message.ShouldBe("TestError2");
        result.Errors.ElementAt(1).Details.ShouldBe("Second error occurred");
    }

    [Fact]
    public void Generic_Failure_With_Message_And_Multiple_Errors_Should_Throw_When_Accessing_Value()
    {
        var errors = new List<Error>
        {
            new("TestError1", "First error occurred"),
            new("TestError2", "Second error occurred")
        };
        var result = Result<TestResponse>.Failure("Failure message", errors);

        result.Succeeded.ShouldBeFalse();

        Should.Throw<InvalidOperationException>(() => _ = result.Value)
            .Message.ShouldBe("You can't access .Value when .Succeeded is false");
    }

    [Fact]
    public void Generic_Result_Should_Be_Implicitly_Convertible_From_Value()
    {
        TestResponse response = new("Test Response");
        Result<TestResponse> result = response;

        result.Succeeded.ShouldBeTrue();
        result.Value.Message.ShouldBe("Test Response");
    }

    [Fact]
    public void Failure_And_Adding_Another_Failure_Should_Have_All_Errors()
    {
        var initialError = new Error("InitialError", "Initial error occurred");
        var additionalError = new Error("AdditionalError", "Additional error occurred");

        var result1 = Result<TestResponse>.Failure(initialError);
        var result2 = Result<TestResponse>.Failure(additionalError);

        // Create a new result from the first result
        var result3 = Result<string>.Failure(result1);
        result3.Succeeded.ShouldBeFalse();
        result3.Errors.Count.ShouldBe(1);

        var result4 = Result.Failure(result1, additionalError);
        result4.Succeeded.ShouldBeFalse();
        result4.Errors.Count.ShouldBe(2);
    }

    [Fact]
    public void Success_And_Adding_A_Failure_Should_Not_Be_Successful()
    {
        var successResult = Result<TestResponse>.Success(new TestResponse("Initial Success"));
        var failureError = new Error("FailureError", "Failure occurred");

        var result = Result.Failure(successResult, failureError);

        result.Succeeded.ShouldBeFalse();
        result.Errors.Count.ShouldBe(1);
        result.Errors.First().Message.ShouldBe("FailureError");
        result.Errors.First().Details.ShouldBe("Failure occurred");
    }

    [Fact]
    public void Multiple_Failures_Should_Be_Combineable()
    {
        var error1 = new Error("Error1", "First error occurred");
        var error2 = new Error("Error2", "Second error occurred");

        var result1 = Result<TestResponse>.Failure(error1);
        var result2 = Result<TestResponse>.Failure(error2);
        var combinedResult = Result<TestResponse>.Failure(result1, result2);

        combinedResult.Succeeded.ShouldBeFalse();
        combinedResult.Errors.Count.ShouldBe(2);
        combinedResult.Errors.ElementAt(0).Message.ShouldBe("Error1");
        combinedResult.Errors.ElementAt(0).Details.ShouldBe("First error occurred");
        combinedResult.Errors.ElementAt(1).Message.ShouldBe("Error2");
        combinedResult.Errors.ElementAt(1).Details.ShouldBe("Second error occurred");
    }

    [Fact]
    public void Generic_Failure_And_Adding_Another_Failure_Should_Have_All_Errors()
    {
        var initialError = new Error("InitialError", "Initial error occurred");
        var additionalError = new Error("AdditionalError", "Additional error occurred");

        var initialResult = Result<TestResponse>.Failure(initialError);

        // Create a new result from the first result
        var result2 = Result<TestResponse>.Failure(initialResult, additionalError);
        result2.Succeeded.ShouldBeFalse();
        result2.Errors.Count.ShouldBe(2);

        var result3 = Result<TestResponse>.Failure(result2, additionalError);
        result3.Succeeded.ShouldBeFalse();
        result3.Errors.Count.ShouldBe(3);
    }
}

record TestResponse(string Message);
