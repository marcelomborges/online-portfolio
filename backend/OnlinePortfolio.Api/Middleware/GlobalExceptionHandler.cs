using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using OnlinePortfolio.Api.Models;

namespace OnlinePortfolio.Api.Middleware;

public sealed class GlobalExceptionHandler(
    IHostEnvironment environment,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception processing {Method} {Path}",
            httpContext.Request.Method,
            httpContext.Request.Path);

        var statusCode = exception switch
        {
            ArgumentException => StatusCodes.Status400BadRequest,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError
        };

        var detail = statusCode == StatusCodes.Status500InternalServerError && !environment.IsDevelopment()
            ? "An unexpected error occurred."
            : exception.Message;

        var response = new ApiErrorResponse(
            Title: GetTitle(statusCode),
            Status: statusCode,
            Detail: detail,
            TraceId: Activity.Current?.Id ?? httpContext.TraceIdentifier);

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
        return true;
    }

    private static string GetTitle(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "Bad Request",
        StatusCodes.Status404NotFound => "Not Found",
        _ => "Internal Server Error"
    };
}
