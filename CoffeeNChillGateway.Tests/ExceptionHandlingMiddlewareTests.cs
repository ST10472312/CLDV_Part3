using CoffeeNChillGateway.Exceptions;
using CoffeeNChillGateway.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoffeeNChillGateway.Tests;

public class ExceptionHandlingMiddlewareTests
{
    private static async Task<(int Status, string Body)> RunAsync(Exception toThrow)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new ExceptionHandlingMiddleware(_ => throw toThrow, NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        return (context.Response.StatusCode, body);
    }

    [Fact]
    public async Task NotFound_Returns404_WithOurMessage()
    {
        var (status, body) = await RunAsync(new NotFoundException("Menu item COF-999 was not found."));
        Assert.Equal(404, status);
        Assert.Contains("COF-999", body);
    }

    [Fact]
    public async Task Conflict_Returns409()
        => Assert.Equal(409, (await RunAsync(new ConflictException("dup"))).Status);

    [Fact]
    public async Task QueueDown_Returns503()
        => Assert.Equal(503, (await RunAsync(new ServiceUnavailableException("down"))).Status);

    [Fact]
    public async Task UnknownError_Returns500_AndHidesInternals()
    {
        var (status, body) = await RunAsync(new InvalidOperationException("secret connection string"));
        Assert.Equal(500, status);
        Assert.DoesNotContain("secret", body);
    }
}