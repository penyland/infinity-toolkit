
using Microsoft.AspNetCore.Mvc;

namespace Infinity.Toolkit;

public class Error(string code, string details)
{
    public static readonly Error None = new("No error occurred.");

    public Error(string details)
        : this(string.Empty, details)
    {
    }

    public string Code { get; } = code ?? string.Empty;

    public string Details { get; } = details;
}

public class ExceptionError(string code, string details, Exception exception) : Error(code, details)
{
    public Exception Exception { get; } = exception ?? throw new ArgumentNullException(nameof(exception));

    // Result result = new Error("Failed to update message");
    //public static implicit operator Result(Error error) => new ErrorResult(error);

    //public static implicit operator ProblemDetails(Error error)
    //{
    //    return new ProblemDetails
    //    {
    //        Title = error.Code,
    //        Detail = error.Details,
    //        Type = error.Code,
    //        Status = 400 // Default to Bad Request
    //    };
    //}

    public static implicit operator ExceptionError(Exception exception) => new(exception.GetType().Name, exception.Message, exception);
}
