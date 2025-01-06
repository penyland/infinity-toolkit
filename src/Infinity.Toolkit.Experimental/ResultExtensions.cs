namespace Infinity.Toolkit.Experimental;

public static class ResultExtensions
{
    public static T? Value<T>(this Result<T> result)
    {
        return result switch
        {
            SuccessResult<T> success => success.Value,
            _ => default
        };
    }

    public static Result<T> Ok<T>(T value) => new SuccessResult<T>(value);

    /// <summary>
    /// Match the result with the provided functions which allows to execute different logic based on the result.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="result">The result to match.</param>
    /// <param name="onSuccess">The function to execute when the result is successful.</param>
    /// <param name="onFailure">The function to execute when the result is a failure.</param>
    public static T Match<T>(this Result result, Func<T> onSuccess, Func<Error, T> onFailure)
    {
        return result.Succeeded ? onSuccess() : onFailure(result.Error);
    }

    public static Result<T> ToResult<T>(this T value) => new SuccessResult<T>(value);
}
