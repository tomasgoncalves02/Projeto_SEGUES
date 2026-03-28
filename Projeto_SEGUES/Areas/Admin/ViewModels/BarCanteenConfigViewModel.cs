namespace Projeto_SEGUES.Areas.Admin.ViewModels;

/// <summary>
/// ViewModel used to configure and display the operational parameters of the Bar and Canteen.
/// </summary>
/// <remarks>
/// This model handles both the raw <see cref="TimeSpan"/> data used for system logic 
/// and formatted strings for easy binding with HTML time-picker inputs.
/// </remarks>
public class BarCanteenConfigViewModel
{
    // ==========================================
    // Bar Configuration
    // ==========================================

    /// <summary>The exact time the Bar opens for service.</summary>
    public TimeSpan? BarOpeningTime { get; set; }

    /// <summary>String representation of the Bar's opening time (e.g., "08:30").</summary>
    public string? BarOpeningTimeString { get; set; }

    /// <summary>The exact time the Bar closes and stops accepting orders.</summary>
    public TimeSpan? BarClosingTime { get; set; }

    /// <summary>String representation of the Bar's closing time (e.g., "21:00").</summary>
    public string? BarClosingTimeString { get; set; }

    /// <summary>External URL or file path for the Bar's digital menu.</summary>
    public string? BarMenuLink { get; set; }

    // ==========================================
    // Canteen Configuration
    // ==========================================

    /// <summary>Start of the Canteen's lunch service window.</summary>
    public TimeSpan? CanteenLunchOpeningTime { get; set; }

    /// <summary>String representation of the lunch opening time.</summary>
    public string? CanteenLunchOpeningTimeString { get; set; }

    /// <summary>End of the Canteen's lunch service window.</summary>
    public TimeSpan? CanteenLunchClosingTime { get; set; }

    /// <summary>String representation of the lunch closing time.</summary>
    public string? CanteenLunchClosingTimeString { get; set; }

    /// <summary>Start of the Canteen's dinner service window.</summary>
    public TimeSpan? CanteenDinnerOpeningTime { get; set; }

    /// <summary>String representation of the dinner opening time.</summary>
    public string? CanteenDinnerOpeningTimeString { get; set; }

    /// <summary>End of the Canteen's dinner service window.</summary>
    public TimeSpan? CanteenDinnerClosingTime { get; set; }

    /// <summary>String representation of the dinner closing time.</summary>
    public string? CanteenDinnerClosingTimeString { get; set; }

    /// <summary>External URL or file path for the Canteen's daily/weekly menu.</summary>
    public string? CanteenMenuLink { get; set; }

    // ==========================================
    // Availability Configuration
    // ==========================================

    /// <summary>Flag indicating if the facilities are operational on Saturdays.</summary>
    public bool IsOpenSaturday { get; set; }

    /// <summary>Flag indicating if the facilities are operational on Sundays.</summary>
    public bool IsOpenSunday { get; set; }
}