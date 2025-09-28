namespace Infinity.Toolkit;

public class ErrorResult : Result, Failure
{
    public ErrorResult(Error error)
        : this(error.Details, [error])
    {
        Succeeded = false;
    }

    public ErrorResult(string message)
        : this(message, [new(message)])
    {
    }

    public ErrorResult(string message, IReadOnlyCollection<Error> errors)
    {
        Message = message;
        Succeeded = false;
        Errors = errors ?? [];
    }

    public ErrorResult(IReadOnlyCollection<Error> errors)
        : this(string.Empty, errors)
    {
    }

    public ErrorResult(Exception exception)
        : this(exception.Message, [new ExceptionError(string.Empty, exception.Message, exception)])
    {
    }

    public string Message { get; }
}

public class ErrorResult<T> : Result<T>, Failure
{
    public ErrorResult(Error error)
        : base(default!, [error])
    {
    }

    public ErrorResult(string message)
        : base(default!, [new Error(message)])
    {
        Message = message;
    }

    public ErrorResult(string message, IReadOnlyCollection<Error> errors)
        : base(default!, errors)
    {
        Message = message;
        Succeeded = false;
        Errors = errors ?? [];
    }

    public ErrorResult(IReadOnlyCollection<Error> errors)
        : base(default!, errors)
    {
        Succeeded = false;
        Errors = errors;
    }

    public ErrorResult(Exception exception)
        : base(default!, [new ExceptionError(exception.GetType().Name, exception.Message, exception)])
    {
        Succeeded = false;
        Message = exception.Message;
    }

    //public IReadOnlyCollection<Error> Errors { get; }

    //public static implicit operator ProblemDetails(ErrorResult<T> errorResult)
    //{
    //    return new ProblemDetails
    //    {
    //        Title = errorResult.Error.Code,
    //        Detail = errorResult.Error.Details,
    //        Type = errorResult.Error.Code,
    //        Status = 400 // Default to Bad Request
    //    };
    //}
    public string Message { get; }
}
