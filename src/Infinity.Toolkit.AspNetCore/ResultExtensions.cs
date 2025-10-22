using Microsoft.AspNetCore.Mvc;

namespace Infinity.Toolkit.AspNetCore;

public static class ResultExtensions
{
    public static ProblemDetails ToProblemDetails(this Result result, int status = 400)
    {
        if (result is Success)
        {
            throw new InvalidOperationException("Unable to convert a SuccessResult to ProblemDetails");
        }

        return new ProblemDetails
        {
            Detail = result.Errors.First().Details,
            Status = status,
            Extensions =
            {
                ["errors"] = result.Errors
            }
        };
    }

    public static ProblemDetails ToProblemDetails<T>(this ErrorResult<T> result, int status = 400)
    {
        if (result is Success)
        {
            throw new InvalidOperationException("Unable to convert a SuccessResult to ProblemDetails");
        }

        return new ProblemDetails
        {
            Title = result.Message,
            //Detail = result.Errors.First().Details,
            Status = status,
            Extensions =
            {
                ["errors"] = result.Errors
            }
        };
    }
}
