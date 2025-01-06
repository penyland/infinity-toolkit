namespace Infinity.Toolkit.Experimental;
#pragma warning restore SA1302 // Interface names should begin with I
#pragma warning restore IDE1006 // Naming Styles

public class SuccessResult : Result, Success
{
    public SuccessResult()
    {
        Succeeded = true;
    }
}

public class SuccessResult<T> : Result<T>, Success
{
    public SuccessResult()
        : base(default(T)!) => Succeeded = true;

    public SuccessResult(T data)
        : base(data)
    {
        Succeeded = true;
    }
}
