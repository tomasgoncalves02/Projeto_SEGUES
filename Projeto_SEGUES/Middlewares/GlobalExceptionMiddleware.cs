using Microsoft.Extensions.Localization;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Resources;

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
            _logger.LogError(ex, 
                Errors.ResourceManager.GetString(nameof(AppErrors.InternalServerError), System.Globalization.CultureInfo.InvariantCulture) ?? "Internal Server Error", 
                "Error",
                TableName.All,
                AppOperation.Other
            );
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

        // Redirect the user to your standard MVC/Razor Error route
        context.Response.Redirect("/Home/Error");
        
        await Task.CompletedTask;
    }
}