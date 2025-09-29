namespace Infinity.Toolkit;

public class Error(string code, string details)
{
    public static readonly Error None = new("No error occurred.");
    public static readonly Error NullValue = new("Error.NullValue", "Value is null");

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

    public static implicit operator ExceptionError(Exception exception) => new(exception.GetType().Name, exception.Message, exception);
}
