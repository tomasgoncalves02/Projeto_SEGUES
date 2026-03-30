using System.ComponentModel.DataAnnotations;
using Projeto_SEGUES.Models.Enums;

namespace Projeto_SEGUES.Areas.Report.ViewModels;

/// <summary>
/// ViewModel used for filtering and displaying ticket search results in the reporting module.
/// </summary>
/// <remarks>
/// This model allows administrators to query the ticketing database using multiple dimensions, 
/// including ticket state (Active/Used), flow type (Consumption/Recharge), and temporal constraints.
/// </remarks>
public class ReportTicketSearchViewModel
{
    /// <summary>
    /// Search string for filtering tickets by user identifier or ticket reference.
    /// </summary>
    [Display(Name = "Pesquisar")]
    public string? SearchString { get; set; }

    /// <summary>
    /// Filter to isolate tickets in a specific state (e.g., Pending, Used, Canceled).
    /// </summary>
    /// <value>A nullable <see cref="TicketState"/> enum value.</value>
    [Display(Name = "Estado")]
    public TicketState? StateFilter { get; set; }

    /// <summary>
    /// Filter to distinguish between different ticket operation flows (e.g., Credit vs. Debit).
    /// </summary>
    /// <value>A nullable <see cref="TicketFlow"/> enum value.</value>
    [Display(Name = "Fluxo")]
    public TicketFlow? FlowFilter { get; set; }

    /// <summary>
    /// Starting date for the search, used to filter results from a specific point in time onwards.
    /// </summary>
    [Display(Name = "A partir de")]
    [DataType(DataType.Date)]
    public DateTime? DateFilter { get; set; }

    /// <summary>
    /// The collection of ticket entities that match the specified search and filter criteria.
    /// </summary>
    /// <remarks>
    /// This property holds the hydrated objects returned from the database to be rendered in the report table.
    /// </remarks>
    public IEnumerable<Models.Ticket.Ticket> Results { get; set; } = new List<Models.Ticket.Ticket>();
}