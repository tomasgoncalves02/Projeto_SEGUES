namespace Projeto_SEGUES.Areas.Admin.ViewModels;

public class BarCanteenConfigViewModel
{
    // Bar
    public TimeSpan? BarOpeningTime { get; set; }
    public string? BarOpeningTimeString { get; set; }
    public TimeSpan? BarClosingTime { get; set; }
    public string? BarClosingTimeString { get; set; }
    public string? BarMenuLink { get; set; }

    // Canteen
    public TimeSpan? CanteenLunchOpeningTime { get; set; }
    public string? CanteenLunchOpeningTimeString { get; set; }
    public TimeSpan? CanteenLunchClosingTime { get; set; }
    public string? CanteenLunchClosingTimeString { get; set; }
    public TimeSpan? CanteenDinnerOpeningTime { get; set; }
    public string? CanteenDinnerOpeningTimeString { get; set; }
    public TimeSpan? CanteenDinnerClosingTime { get; set; }
    public string? CanteenDinnerClosingTimeString { get; set; }
    public string? CanteenMenuLink { get; set; }
    public bool IsOpenSaturday { get; set; }
    public bool IsOpenSunday { get; set; }
}