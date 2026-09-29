using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CinemaShowtimesApi.Errors;

public sealed class ApiExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not AppException appException)
        {
            return false;
        }

        httpContext.Response.StatusCode = appException.StatusCode;

        var problem = new ProblemDetails
        {
            Status = appException.StatusCode,
            Title = appException.Title,
            Detail = appException.Message,
            Type = $"https://httpstatuses.com/{appException.StatusCode}",
            Instance = httpContext.Request.Path
        };
        problem.Extensions["errorCode"] = appException.ErrorCode;

        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
