using System.ComponentModel.DataAnnotations;
using Projeto_SEGUES.Models.Payment;

namespace Projeto_SEGUES.Areas.Report.ViewModels;

/// <summary>
/// ViewModel used for filtering and displaying financial transaction search results in the reporting module.
/// </summary>
/// <remarks>
/// This model provides the necessary criteria to query the transaction history, 
/// supporting text-based searches, chronological filtering, and categorization by transaction type.
/// </remarks>
public class ReportTransactionSearchViewModel
{
    /// <summary>
    /// Search string for filtering transactions by user name, reference ID, or description.
    /// </summary>
    public string? SearchString { get; set; }

    /// <summary>
    /// Date filter used to narrow down the transaction history to a specific day.
    /// </summary>
    [DataType(DataType.Date)]
    public DateTime? DateFilter { get; set; }

    /// <summary>
    /// Filter to isolate specific types of transactions (e.g., Deposit, Purchase, Refund).
    /// </summary>
    public string? TypeFilter { get; set; }

    /// <summary>
    /// The collection of transaction entities that match the specified search and filter criteria.
    /// </summary>
    /// <remarks>
    /// This property holds the hydrated data from the database to be displayed in the audit logs.
    /// </remarks>
    public IEnumerable<Transaction> Results { get; set; } = new List<Transaction>();
}