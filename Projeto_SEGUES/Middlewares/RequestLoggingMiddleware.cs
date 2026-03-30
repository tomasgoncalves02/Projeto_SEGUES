using System.Security.Claims;
using Serilog.Context;

namespace Projeto_SEGUES.Middlewares;

/// <summary>
/// Middleware responsible for enriching all logs within a request's scope with user and path metadata.
/// </summary>
/// <remarks>
/// By intercepting the request early in the pipeline, this middleware pushes the User ID and 
/// Request Path into the Serilog <c>LogContext</c>, making it possible to trace all subsequent 
/// logs back to a specific user session or endpoint.
/// </remarks>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestLoggingMiddleware"/>.
    /// </summary>
    public RequestLoggingMiddleware(RequestDelegate next) => _next = next;

    /// <summary>
    /// Extracts security claims and enriches the logging context before proceeding with the pipeline.
    /// </summary>
    /// <param name="context">The current <see cref="HttpContext"/>.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        // Retrieve the unique identifier of the authenticated user from the claims.
        string? userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        string requestPath = context.Request.Path;

        // Ensure empty strings are treated as null for cleaner log data.
        if (string.IsNullOrEmpty(userId)) userId = null;

        // Use Serilog's PushProperty to wrap the entire request execution in a diagnostic context.
        using (LogContext.PushProperty("AppUserId", userId))
        using (LogContext.PushProperty("RequestPath", requestPath))
        {
            // Execute the next middleware/controller in the pipeline.
            await _next(context);
        }
    }
}