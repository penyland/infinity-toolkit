namespace Infinity.Toolkit.Experimental;

public abstract class Result
{
    public bool Succeeded { get; protected set; }

    public bool Failed => !Succeeded;

    public Error Error { get; protected set; }

    public static Result Success() => new SuccessResult();

    public static Result<T> Success<T>(T data) => new SuccessResult<T>(data);

    public static Result<T> Success<T>() => new SuccessResult<T>();

    public static Result Failure(string message) => new ErrorResult(message);

    public static Result<T> Failure<T>(string details) => new ErrorResult<T>(details);

    public static Result<T> Failure<T>(Error error) => new ErrorResult<T>(error);

    public static Result<T> Failure<T>(string message, IReadOnlyCollection<Error> errors) => new ErrorResult<T>(message, errors);
}

public abstract class Result<T> : Result
{
    private T value;

    protected Result(T data)
    {
        Value = data;
    }

    protected Result(Error error)
    {
        Error = error;
    }

    public T Value
    {
        get => Succeeded ? value : throw new InvalidOperationException($"You can't access .{nameof(Value)} when .{nameof(Succeeded)} is false");
        set => this.value = value;
    }

    public static implicit operator Result<T>(T value) => new SuccessResult<T>(value);

    public static implicit operator Result<T>(Error error) => new ErrorResult<T>(error);

    public static implicit operator T(Result<T> result) => result.Value;

    public TResult Switch<TResult>(Func<T, TResult> onSuccess, Func<Error, TResult> onFailure)
    {
        return Succeeded ? onSuccess(Value!) : onFailure(Error);
    }
}

#pragma warning disable IDE1006 // Naming Styles
public interface Success { }
public interface Failure
#pragma warning restore IDE1006 // Naming Styles
{
    string Message { get; }

    IReadOnlyCollection<Error> Errors { get; }
}
