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
    public string? BarOpeningTimeString => BarOpeningTime?.ToString(@"hh\:mm");

    /// <summary>The exact time the Bar closes and stops accepting orders.</summary>
    public TimeSpan? BarClosingTime { get; set; }

    /// <summary>String representation of the Bar's closing time (e.g., "21:00").</summary>
    public string? BarClosingTimeString => BarClosingTime?.ToString(@"hh\:mm");

    /// <summary>External URL or file path for the Bar's digital menu.</summary>
    public string? BarMenuLink { get; set; }

    // ==========================================
    // Canteen Configuration
    // ==========================================

    /// <summary>Start of the Canteen's lunch service window.</summary>
    public TimeSpan? CanteenLunchOpeningTime { get; set; }

    /// <summary>String representation of the lunch opening time.</summary>
    public string? CanteenLunchOpeningTimeString => CanteenLunchOpeningTime?.ToString(@"hh\:mm");

    /// <summary>End of the Canteen's lunch service window.</summary>
    public TimeSpan? CanteenLunchClosingTime { get; set; }

    /// <summary>String representation of the lunch closing time.</summary>
    public string? CanteenLunchClosingTimeString => CanteenLunchClosingTime?.ToString(@"hh\:mm");

    /// <summary>Start of the Canteen's dinner service window.</summary>
    public TimeSpan? CanteenDinnerOpeningTime { get; set; }

    /// <summary>String representation of the dinner opening time.</summary>
    public string? CanteenDinnerOpeningTimeString => CanteenDinnerOpeningTime?.ToString(@"hh\:mm");

    /// <summary>End of the Canteen's dinner service window.</summary>
    public TimeSpan? CanteenDinnerClosingTime { get; set; }

    /// <summary>String representation of the dinner closing time.</summary>
    public string? CanteenDinnerClosingTimeString => CanteenDinnerClosingTime?.ToString(@"hh\:mm");

    /// <summary>External URL or file path for the Canteen's daily/weekly menu.</summary>
    public string? CanteenMenuLink { get; set; }
    
    // ==========================================
    // Availability Configuration
    // ==========================================

    /// <summary>Flag indicating if the facilities are operational on Saturdays.</summary>
    public bool IsOpenSaturday { get; set; }

    /// <summary>Flag indicating if the facilities are operational on Sundays.</summary>
    public bool IsOpenSunday { get; set; }

    /// <summary>Calculates if the Lunch service is currently active based on system time.</summary>
    public bool IsLunchOpenNow => IsNowWithinRange(CanteenLunchOpeningTime, CanteenLunchClosingTime);

    /// <summary>Calculates if the Dinner service is currently active based on system time.</summary>
    public bool IsDinnerOpenNow => IsNowWithinRange(CanteenDinnerOpeningTime, CanteenDinnerClosingTime);

    /// <summary>
    /// Determines if today is an operational workday based on Saturday/Sunday configurations.
    /// </summary>
    public bool IsWorkDay => DateTime.Now.DayOfWeek switch
    {
        DayOfWeek.Saturday => IsOpenSaturday,
        DayOfWeek.Sunday => IsOpenSunday,
        _ => true // Default for Monday through Friday
    };

    /// <summary>
    /// Global state for the Canteen: True if it is a Workday AND either Lunch or Dinner is active.
    /// </summary>
    public bool IsCanteenOpenNow => (IsLunchOpenNow || IsDinnerOpenNow) && IsWorkDay;

    /// <summary>
    /// Helper method to check if the current server time falls between two specific timeframes.
    /// </summary>
    /// <param name="start">The opening time.</param>
    /// <param name="end">The closing time.</param>
    /// <returns>True if the current time is between start and end; otherwise false.</returns>
    private static bool IsNowWithinRange(TimeSpan? start, TimeSpan? end)
    {
        if (!start.HasValue || !end.HasValue) return false;
        var currentTime = DateTime.Now.TimeOfDay;
        return currentTime >= start.Value && currentTime <= end.Value;
    }
}