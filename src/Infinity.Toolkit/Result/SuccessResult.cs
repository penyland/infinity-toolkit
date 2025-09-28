using System.Diagnostics;

namespace Infinity.Toolkit;

public class SuccessResult : Result, Success
{
    public SuccessResult() : base()
    {
    }
}

[DebuggerDisplay("Value = {Value}")]
public class SuccessResult<T> : Result<T>, Success
{
    public SuccessResult()
        : base(default!, []) => Succeeded = true;

    public SuccessResult(T data)
        : base(data, []) => Succeeded = true;
}
