
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
