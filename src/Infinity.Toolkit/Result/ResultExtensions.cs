namespace Infinity.Toolkit;

public static class ResultExtensions
{
    public static T? Value<T>(this Result<T> result)
    {
        return result switch
        {
            SuccessResult<T> success => success.Value,
            ErrorResult<T> => throw new InvalidOperationException($"You can't access .{nameof(Result<>.Value)} when .{nameof(Result.Succeeded)} is false"),
            _ => default
        };
    }

    /// <summary>
    /// Match the result with the provided functions which allows to execute different logic based on the result.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="result">The result to match.</param>
    /// <param name="onSuccess">The function to execute when the result is successful.</param>
    /// <param name="onFailure">The function to execute when the result is a failure.</param>
    public static T Match<T>(this Result result, Func<T> onSuccess, Func<IReadOnlyCollection<Error>, T> onFailure)
    {
        return result.Succeeded ? onSuccess() : onFailure(result.Errors);
    }

    public static Result<T> ToResult<T>(this T value) => new SuccessResult<T>(value);
}
