namespace Projeto_SEGUES.Areas.Report.ViewModels;

/// <summary>
/// Data Transfer Object (DTO) used for representing generalized chart data.
/// </summary>
/// <remarks>
/// This model follows the standard {Label, Count} pattern required by most JavaScript 
/// visualization libraries (like Chart.js), making it highly versatile for various types 
/// of statistical reports.
/// </remarks>
public class ChartDataDto
{
    /// <summary>
    /// The descriptive label for the data point (e.g., "Monday", "Product A", "Active Users").
    /// </summary>
    public string Label { get; set; } = "";

    /// <summary>
    /// The numerical value or frequency associated with the label.
    /// </summary>
    /// <value>A non-negative integer representing the magnitude of the data point.</value>
    public int Count { get; set; }
}