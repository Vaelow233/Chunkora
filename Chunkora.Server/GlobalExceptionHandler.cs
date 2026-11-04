using Chunkora.Server.Errors;
using Chunkora.Server.Utils;
using Microsoft.AspNetCore.Diagnostics;

namespace Chunkora.Server;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception occurred.");
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        var response = Result<object?>.Failure(GlobalErrors.OfException(exception));
        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
        return true;
    }
}