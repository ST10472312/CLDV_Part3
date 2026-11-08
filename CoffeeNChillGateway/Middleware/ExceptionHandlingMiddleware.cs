using System.Text.Json;
using CoffeeNChillGateway.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChillGateway.Middleware;

/// <summary>
/// Catches every unhandled exception and returns RFC 7807 "problem details" JSON.
/// One place decides status codes, so controllers stay free of try/catch blocks and
/// stack traces never leak to clients.
/// </summary>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex) when (!context.Response.HasStarted)   // cannot rewrite a response already sent
        {
            await WriteProblemAsync(context, ex);
        }
    }

    private async Task WriteProblemAsync(HttpContext context, Exception ex)
    {
        var (status, title) = ex switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            BadRequestException => (StatusCodes.Status400BadRequest, "Bad request"),
            BadHttpRequestException => (StatusCodes.Status400BadRequest, "Bad request"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            ServiceUnavailableException => (StatusCodes.Status503ServiceUnavailable, "Dependency unavailable"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };

        if (status >= 500)
            logger.LogError(ex, "Unhandled error on {Method} {Path}", context.Request.Method, context.Request.Path);
        else
            logger.LogWarning("{Status} on {Method} {Path}: {Message}", status, context.Request.Method, context.Request.Path, ex.Message);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            // 500s never reveal internals; every other code shows our own safe message.
            Detail = status == StatusCodes.Status500InternalServerError ? "Please try again or contact support." : ex.Message,
            Instance = context.Request.Path
        };
        problem.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }
}