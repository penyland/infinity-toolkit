using Infinity.Toolkit.Experimental.Results;

var result1 = Result<int>.Success(42);

Console.WriteLine($"Success Value: {result1.Value}");

var failure1 = Result<int>.Failure(new BadRequestError("Invalid input"));
Console.WriteLine($"Failure - Error[1]: {failure1.Errors[0].Description}");

var multipleErrors = Result<int>.Failure(
    new ValidationError("Field A is required"),
    new ValidationError("Field B must be a number")
);

Console.WriteLine($"Failure - Error Count: {multipleErrors.Errors.Count}");
foreach (var error in multipleErrors.Errors)
{
    Console.WriteLine($"Failure - Error: {error.Description}");
}

// Exception handling example
var exception = new Exception("Something went wrong");
var failureWithException = Result.Failure<int>(exception);
Console.WriteLine($"Failure with Exception - Error Count: {failureWithException.Errors.Count}");
Console.WriteLine($"Failure with Exception - Error: {failureWithException.Errors[0].Description}");

var addErrorToResult = Result.Failure<int>(failureWithException, new NotFoundError("Resource not found"));
Console.WriteLine($"Failure with Added Error - Error Count: {addErrorToResult.Errors.Count}");
foreach (var error in addErrorToResult.Errors)
{
    Console.WriteLine($"Failure with Added Error - Error: {error.Description}");
}


// Implicit conversion example
int result2 = Result<int>.Success(100);
Console.WriteLine($"Implicit Conversion - Success Value: {result2}");
