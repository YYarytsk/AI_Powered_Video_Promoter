using System.Net;
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

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (NotImplementedException ex)
        {
            _logger.LogWarning(ex, "Feature not implemented: {Message}", ex.Message);
            context.Response.StatusCode = (int)HttpStatusCode.NotImplemented;
            context.Response.ContentType = "application/json";
            var error = new { message = ex.Message, type = "FeatureNotImplemented" };
            await context.Response.WriteAsync(JsonSerializer.Serialize(error));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";
            var error = new { message = "An unexpected error occurred.", type = ex.GetType().Name };
            await context.Response.WriteAsync(JsonSerializer.Serialize(error));
        }
    }
}
