using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Resources;

namespace Projeto_SEGUES.Extensions;

/// <summary>
/// Extension methods for the <see cref="AppErrors"/> enum to handle localized error messaging.
/// </summary>
/// <remarks>
/// This utility separates concerns between internal system logging (invariant culture) 
/// and user-facing display (UI culture), centralizing the retrieval of resources.
/// </remarks>
public static class AppErrorsExtensions
{
    /// <summary>
    /// Core logic to retrieve a string from the resource manager based on the specific error enum and culture.
    /// </summary>
    /// <param name="appError">The specific error code.</param>
    /// <param name="culture">The target culture for translation.</param>
    /// <returns>The translated error string or the enum name as a fallback.</returns>
    private static string GetErrorMessage(this AppErrors appError, System.Globalization.CultureInfo culture)
    {
        return Errors.ResourceManager.GetString(appError.ToString(), culture) ?? appError.ToString();
    }

    /// <summary>
    /// Retrieves the error message in the Invariant Culture for logging purposes.
    /// </summary>
    /// <remarks>
    /// Using the Invariant Culture for logs ensures that technical diagnostics remain 
    /// consistent and searchable regardless of the server's regional settings.
    /// </remarks>
    /// <param name="appError">The error code to log.</param>
    /// <returns>A standardized English/Technical error message.</returns>
    public static string GetLogErrorMessage(this AppErrors appError)
    {
        return GetErrorMessage(appError, System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Retrieves the error message in the user's current UI culture for display in Views.
    /// </summary>
    /// <remarks>
    /// This method is typically used in Controllers and Views to present a user-friendly, 
    /// translated explanation of what went wrong.
    /// </remarks>
    /// <param name="appError">The error code to display.</param>
    /// <returns>A localized error message.</returns>
    public static string GetViewErrorMessage(this AppErrors appError)
    {
        return GetErrorMessage(appError, System.Globalization.CultureInfo.CurrentUICulture);
    }
}