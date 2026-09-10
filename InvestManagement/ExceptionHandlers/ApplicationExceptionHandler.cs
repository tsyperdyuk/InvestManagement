using InvestManagement.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace InvestManagement.Api.ExceptionHandlers;

public sealed class ApplicationExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var statusCode = exception switch
        {
            NotFoundException => StatusCodes.Status404NotFound,
            ConflictException => StatusCodes.Status409Conflict,
            BusinessRuleException => StatusCodes.Status422UnprocessableEntity,
            _ => 0
        };

        if (statusCode == 0)
        {
            return false;
        }

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = statusCode,
                Title = GetTitle(statusCode),
                Detail = exception.Message,
                Instance = httpContext.Request.Path
            },
            cancellationToken);

        return true;
    }

    private static string GetTitle(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status404NotFound => "Resource not found",
            StatusCodes.Status409Conflict => "Request conflicts with current state",
            StatusCodes.Status422UnprocessableEntity => "Business rule violation",
            _ => "Request failed"
        };
    }
}
