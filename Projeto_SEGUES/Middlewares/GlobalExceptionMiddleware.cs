using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Enums;

namespace Projeto_SEGUES.Middlewares;

/// <summary>
/// Global middleware responsible for capturing and handling all unhandled exceptions within the request pipeline.
/// </summary>
/// <remarks>
/// This acts as the final safety net of the application. It ensures that any failure in the 
/// Controller or Service layers is logged structuredly and the user is redirected to a friendly error page.
/// </remarks>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalExceptionMiddleware"/>.
    /// </summary>
    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware logic during the HTTP request lifecycle.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Proceed with the request pipeline execution.
            await _next(context);
        }
        catch (Exception ex)
        {
            // Log the error using the application's structured logging extension.
            _logger.LogAppError(AppErrors.InternalServerError, TableName.All, AppOperation.Other, ex);

            // Redirect the user based on the request type (Standard vs. AJAX/HTMX).
            await HandleExceptionAsync(context);
        }
    }

    /// <summary>
    /// Determines the appropriate redirection strategy based on the request headers.
    /// </summary>
    private async Task HandleExceptionAsync(HttpContext context)
    {
        // Safety check: if the response stream has already begun, we cannot modify headers.
        if (context.Response.HasStarted)
        {
            return;
        }

        // Wipe any partial data written to the response buffer.
        context.Response.Clear();

        // Identify the source of the request to handle partial page updates vs. full page loads.
        bool isHtmxRequest = context.Request.Headers["HX-Request"] == "true";
        bool isAjaxRequest = context.Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        if (isHtmxRequest || isAjaxRequest)
        {
            // For HTMX/AJAX, we use a custom header to trigger a client-side redirect.
            // A 200 OK status is used to ensure the client-side library processes the header correctly.
            context.Response.Headers["HX-Redirect"] = "/Home/Error";
            context.Response.StatusCode = StatusCodes.Status200OK;
        }
        else
        {
            // For standard browser navigation, perform a 302 Redirect to the Error view.
            context.Response.Redirect("/Home/Error");
        }

        await Task.CompletedTask;
    }
}