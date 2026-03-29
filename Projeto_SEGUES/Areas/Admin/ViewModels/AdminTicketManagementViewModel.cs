using Projeto_SEGUES.Areas.Report.ViewModels;
using Projeto_SEGUES.Models.Ticket;

namespace Projeto_SEGUES.Areas.Admin.ViewModels;

/// <summary>
/// Composite ViewModel for the centralized administration of ticketing rules and cafeteria schedules.
/// </summary>
/// <remarks>
/// This model aggregates global system settings (Schedules, Validity) with 
/// transactional data (<see cref="Prices"/>) and filtering logic (<see cref="SearchModel"/>) 
/// to provide a comprehensive management interface.
/// </remarks>
public class AdminTicketManagementViewModel
{
    // ==========================================
    // Service Schedule Configuration
    // ==========================================

    /// <summary>The start of the lunch service window (e.g., "12:00").</summary>
    public string LunchOpeningTime { get; set; } = "";

    /// <summary>The end of the lunch service window (e.g., "14:30").</summary>
    public string LunchClosingTime { get; set; } = "";

    /// <summary>The start of the dinner service window (e.g., "19:00").</summary>
    public string DinnerOpeningTime { get; set; } = "";

    /// <summary>The end of the dinner service window (e.g., "21:00").</summary>
    public string DinnerClosingTime { get; set; } = "";

    // ==========================================
    // Pricing & Asset Lifecycle
    // ==========================================

    /// <summary>A collection of current <see cref="TicketPrice"/> records for all user categories.</summary>
    public List<TicketPrice> Prices { get; set; } = [];

    /// <summary>The default number of days a ticket remains valid after purchase.</summary>
    public int CurrentValidityDays { get; set; }

    // ==========================================
    // Reporting & Filtering
    // ==========================================

    /// <summary>
    /// Nested ViewModel handling search criteria for ticket-related reporting.
    /// Uses <see cref="ReportTicketSearchViewModel"/> to maintain cross-area consistency.
    /// </summary>
    public ReportTicketSearchViewModel SearchModel { get; set; } = new();
}