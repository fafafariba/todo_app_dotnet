using Microsoft.AspNetCore.Diagnostics;

namespace Api.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        Console.Error.WriteLine("[error] Unhandled exception: {0}\n{1}", exception.Message, exception.StackTrace);

        httpContext.Response.StatusCode = 500;
        await httpContext.Response.WriteAsJsonAsync(
            new { error = "Internal server error" },
            cancellationToken);

        return true;
    }
}
