using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Resources;

namespace Projeto_SEGUES.Extensions;

public static class AppErrorsExtensions
{
    private static string GetErrorMessage(this AppErrors appError, System.Globalization.CultureInfo culture)
    {
        return Errors.ResourceManager.GetString(appError.ToString(), culture) ?? appError.ToString();
    }
    
    public static string GetLogErrorMessage(this AppErrors appError)
    {
        return GetErrorMessage(appError, System.Globalization.CultureInfo.InvariantCulture);
    }
    
    public static string GetViewErrorMessage(this AppErrors appError)
    {
        return GetErrorMessage(appError, System.Globalization.CultureInfo.CurrentUICulture);
    }
}