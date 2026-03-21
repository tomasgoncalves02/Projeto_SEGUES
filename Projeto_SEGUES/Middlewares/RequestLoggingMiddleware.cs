using System.Security.Claims;
using Serilog.Context;

namespace Projeto_SEGUES.Middlewares;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public RequestLoggingMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        string? userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        string requestPath = context.Request.Path;

        using (LogContext.PushProperty("AppUserId", userId))
        using (LogContext.PushProperty("RequestPath", requestPath))
        {
            await _next(context);
        }
    }
}