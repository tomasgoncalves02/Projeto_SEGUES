using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Enums;

namespace Projeto_SEGUES.Middlewares;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(
        RequestDelegate next, 
        ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Run app. If any controller or service throws an unhandled exception, it bubbles up to here.
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogAppError(AppErrors.InternalServerError, TableName.All, AppOperation.Other, ex);
            
            // Handle the HTTP Response back to the user
            await HandleExceptionAsync(context);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context)
    {
        // If the server has already started sending HTML back to the browser, 
        // we can no longer send a redirect header. We must abort safely.
        if (context.Response.HasStarted)
        {
            return;
        }
        
        // Clear any existing response data that might have been partially written.
        context.Response.Clear();

        // Check if the request is an HTMX or Ajax request
        bool isHtmxRequest = context.Request.Headers["HX-Request"] == "true";
        bool isAjaxRequest = context.Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        if (isHtmxRequest || isAjaxRequest)
        {
            // Tell HTMX to intercept and perform a full-page client-side redirect.
            context.Response.Headers["HX-Redirect"] = "/Home/Error";
            
            // HTMX needs a 200 OK status code to read the header properly.
            context.Response.StatusCode = StatusCodes.Status200OK;
        }
        else
        {
            // Standard full-page browser request, redirect the user to the standard MVC/Razor Error route
            context.Response.Redirect("/Home/Error");
        }
        
        await Task.CompletedTask;
    }
}