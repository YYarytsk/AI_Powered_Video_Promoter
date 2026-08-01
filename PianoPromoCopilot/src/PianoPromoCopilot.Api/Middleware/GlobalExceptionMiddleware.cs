using System.Net;
using System.Runtime.ExceptionServices;
using System.Text.Json;

namespace PianoPromoCopilot.Api.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>Nginx's convention for "client closed the request". Nothing is sent to the client.</summary>
    private const int ClientClosedRequest = 499;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        // A client that navigates away or a cancelled fetch aborts the request. That is normal
        // traffic, not a server fault: logging it at Error hides real failures in the noise, and
        // there is nobody left to read a 500.
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogDebug("Request aborted by the client: {Method} {Path}",
                context.Request.Method, context.Request.Path);

            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = ClientClosedRequest;
            }
        }
        catch (NotImplementedException ex)
        {
            _logger.LogWarning(ex, "Feature not implemented: {Message}", ex.Message);
            await WriteErrorAsync(
                context, ex, HttpStatusCode.NotImplemented,
                new { message = ex.Message, type = "FeatureNotImplemented" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}: {Message}",
                context.Request.Method, context.Request.Path, ex.Message);

            await WriteErrorAsync(
                context, ex, HttpStatusCode.InternalServerError,
                new { message = "An unexpected error occurred.", type = ex.GetType().Name });
        }
    }

    /// <summary>
    /// Writes the error body, unless the response is already on the wire. Assigning StatusCode
    /// after the headers have been sent throws, which would replace the real exception with a
    /// misleading InvalidOperationException and lose the original cause. In that case the only
    /// honest thing to do is rethrow and let the server abort the connection - the exception has
    /// already been logged above.
    /// </summary>
    private static async Task WriteErrorAsync(
        HttpContext context,
        Exception original,
        HttpStatusCode statusCode,
        object body)
    {
        if (context.Response.HasStarted)
        {
            // Rethrow preserving the original stack trace.
            ExceptionDispatchInfo.Capture(original).Throw();
        }

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(body));
    }
}
