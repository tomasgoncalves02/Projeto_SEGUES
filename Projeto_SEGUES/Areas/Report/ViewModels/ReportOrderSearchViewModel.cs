using System.ComponentModel.DataAnnotations;
using Projeto_SEGUES.Models.Enums;

namespace Projeto_SEGUES.Areas.Report.ViewModels;

/// <summary>
/// ViewModel used for filtering and displaying order search results in the reporting module.
/// </summary>
/// <remarks>
/// This model encapsulates the search criteria (text, date, status) and carries the 
/// resulting collection of orders to be rendered in the report tables.
/// </remarks>
public class ReportOrderSearchViewModel
{
    /// <summary>
    /// Gets or sets the search string for filtering orders by buyer name or other text-based identifiers.
    /// </summary>
    [Display(Name = "Pesquisar")]
    public string? SearchString { get; set; }

    /// <summary>
    /// Gets or sets the date filter to narrow down results to a specific calendar day.
    /// </summary>
    [Display(Name = "No Dia")]
    [DataType(DataType.Date)]
    public DateTime? DateFilter { get; set; }

    /// <summary>
    /// Gets or sets the status filter to isolate orders in a specific state (e.g., Pending, Delivered).
    /// </summary>
    /// <value>A nullable <see cref="OrderStatus"/> enum value.</value>
    [Display(Name = "Estado")]
    public OrderStatus? StatusFilter { get; set; }

    /// <summary>
    /// The collection of order entities that match the specified search and filter criteria.
    /// </summary>
    public IEnumerable<Models.Order.Order> Results { get; set; } = new List<Models.Order.Order>();
}