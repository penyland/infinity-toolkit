namespace Infinity.Toolkit;

public class ErrorResult : Result, Failure
{
    public ErrorResult(Error error)
    {
        Errors = [error];
        Message = error.Details;
    }

    public ErrorResult(string message)
        : this(message, [])
    {
    }

    public ErrorResult(string message, IReadOnlyCollection<Error> errors)
    {
        Message = message;
        Succeeded = false;
        Errors = errors ?? [];
    }

    public ErrorResult(IReadOnlyCollection<Error> errors)
    {
        Succeeded = false;
        Errors = errors;
    }

    public string Message { get; }

    public IReadOnlyCollection<Error> Errors { get; }
}
public class ErrorResult<T> : Result<T>, Failure
{
    public ErrorResult(Error error)
        : base(error)
    {
        Errors = [error];
        Message = error.Details;
    }

    public ErrorResult(string message)
        : this(message, [])
    {
    }

    public ErrorResult(string message, IReadOnlyCollection<Error> errors)
        : base(new Error(message))
    {
        Message = message;
        Succeeded = false;
        Errors = errors ?? [];
    }

    public string Message { get; set; }

    public IReadOnlyCollection<Error> Errors { get; }
}

public class Error(string code, string details)
{
    public Error(string details)
        : this(string.Empty, details)
    {
    }

    public string Code { get; } = code ?? string.Empty;

    public string Details { get; } = details;

    // Result result = new Error("Failed to update message");
    public static implicit operator Result(Error error) => new ErrorResult(error);
}
