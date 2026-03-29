using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Api.Common.Errors;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment hostEnvironment,
    ProblemDetailsFactory problemDetailsFactory) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, type) = MapException(exception);

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning(exception, "Request failed for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }

        var detail = ShouldExposeExceptionDetail(hostEnvironment)
            ? exception.Message
            : null;

        var problemDetails = problemDetailsFactory.CreateProblemDetails(
            httpContext,
            statusCode: statusCode,
            title: title,
            type: type,
            detail: detail,
            instance: httpContext.Request.Path);

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }

    private static bool ShouldExposeExceptionDetail(IHostEnvironment hostEnvironment)
    {
        return hostEnvironment.IsDevelopment();
    }

    private static (int StatusCode, string Title, string Type) MapException(Exception exception)
    {
        return exception switch
        {
            ValidationException => (
                StatusCodes.Status400BadRequest,
                "Validation failed.",
                "https://httpstatuses.io/400"),

            BadHttpRequestException => (
                StatusCodes.Status400BadRequest,
                "Bad request.",
                "https://httpstatuses.io/400"),

            KeyNotFoundException => (
                StatusCodes.Status404NotFound,
                "Resource not found.",
                "https://httpstatuses.io/404"),

            DbUpdateException => (
                StatusCodes.Status409Conflict,
                "The request could not be completed because of a data conflict.",
                "https://httpstatuses.io/409"),

            TimeoutException => (
                StatusCodes.Status503ServiceUnavailable,
                "The operation timed out.",
                "https://httpstatuses.io/503"),

            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.",
                "https://httpstatuses.io/500")
        };
    }
}
