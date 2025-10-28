using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Infinity.Toolkit;

public class Error(string code, string details, ErrorType type = ErrorType.Failure)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.None);
    public static readonly Error NullValue = new("NullValue", "Value is null", ErrorType.Validation);
    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);

    public Error(string details)
        : this(string.Empty, details, ErrorType.None)
    {
    }

    public string Code { get; } = code ?? string.Empty;

    public string Details { get; } = details;

    [JsonIgnore]
    public ErrorType Type { get; } = type;
}

public class ExceptionError(string code, string details, Exception exception) : Error(code, details, ErrorType.Failure)
{
    public Exception Exception { get; } = exception ?? throw new ArgumentNullException(nameof(exception));

    public static implicit operator ExceptionError(Exception exception) => new(exception.GetType().Name, exception.Message, exception);
}


public class ValidationError(string propertyName, string errorMessage) : Error(string.Empty, errorMessage, ErrorType.Validation)
{
    public string PropertyName { get; } = propertyName;
    public string ErrorMessage { get; } = errorMessage;
}

public enum ErrorType
{
    [Description("No Warning and Error")]
    None = 100,

    [Description("Bad Request")]
    Validation = 400,

    [Description("Unauthorized")]
    Unauthorized = 401,

    [Description("Forbidden")]
    Forbidden = 403,

    [Description("Not Found")]
    NotFound = 404,

    [Description("Conflict")]
    Conflict = 409,

    [Description("Internal Server Error")]
    Failure = 500,

    [Description("Unprocessable Entity ")]
    UnprocessableEntity = 422,

    [Description("Too Many Requests")]
    TooManyRequests = 429,

    [Description("Service Unavailable")]
    ServiceUnavailable = 503
}
