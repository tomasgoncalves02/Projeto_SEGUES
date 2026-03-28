using Projeto_SEGUES.Areas.Report.ViewModels;

namespace Projeto_SEGUES.Areas.Admin.ViewModels;

/// <summary>
/// ViewModel used for the administrative management of orders and global establishment settings.
/// </summary>
/// <remarks>
/// This model aggregates the physical operating hours of the Bar/Canteen with 
/// a nested search model, allowing administrators to manage the shop's availability 
/// and audit orders from a single interface.
/// </remarks>
public class AdminOrderManagementViewModel
{
    /// <summary>Formatted string representing the Bar's opening time (e.g., "08:00").</summary>
    public string BarOpeningTimeString { get; set; } = "";

    /// <summary>Formatted string representing the Bar's closing time (e.g., "20:00").</summary>
    public string BarClosingTimeString { get; set; } = "";

    /// <summary>Indicates if the establishment accepts orders on Saturdays.</summary>
    public bool IsOpenSaturday { get; set; }

    /// <summary>Indicates if the establishment accepts orders on Sundays.</summary>
    public bool IsOpenSunday { get; set; }

    /// <summary>
    /// Nested ViewModel for handling order filtering and search results.
    /// </summary>
    /// <remarks>
    /// By nesting the <see cref="ReportOrderSearchViewModel"/>, the admin interface 
    /// reuses the same filtering logic used in the standard reporting area.
    /// </remarks>
    public ReportOrderSearchViewModel SearchModel { get; set; } = new();
}