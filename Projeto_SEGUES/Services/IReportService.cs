using Projeto_SEGUES.Areas.Report.ViewModels;
using Projeto_SEGUES.Models.Order;
using Projeto_SEGUES.Models.Payment;

namespace Projeto_SEGUES.Services;

/// <summary>
/// Interface for the Reporting and Statistics Service.
/// Provides methods to aggregate operational data for dashboards and 
/// retrieve historical records for orders and financial transactions.
/// </summary>
public interface IReportService
{
    #region Statistics (Dashboards)

    /// <summary>
    /// Aggregates consumption statistics for canteen meal tickets.
    /// Used by the Chart.js frontend to render usage trends.
    /// </summary>
    /// <param name="period">The time period filter (e.g., 1 for Today, 2 for Week, etc.).</param>
    /// <returns>A DTO containing totals, category breakdowns, and chart data points.</returns>
    Task<ReportStatisticsTicketDto> GetTicketsStats(int period = 1);

    /// <summary>
    /// Aggregates sales statistics for bar orders.
    /// Provides insights into revenue, top products, and peak ordering times.
    /// </summary>
    /// <param name="period">The time period filter (e.g., 1 for Today, 2 for Week, etc.).</param>
    /// <returns>A DTO containing revenue metrics, product rankings, and chart labels.</returns>
    Task<ReportStatisticsOrderDto> GetOrdersStats(int period = 1);

    #endregion

    #region History & Auditing

    /// <summary>
    /// Retrieves the order history for a specific customer with optional filters.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="model">Filtering criteria such as date range or status.</param>
    /// <returns>A list of orders belonging to the specified user.</returns>
    Task<List<Order>> GetOrderHistoryAsync(string userId, ReportOrderSearchViewModel model);

    /// <summary>
    /// Retrieves a global order history for administrative auditing.
    /// </summary>
    /// <param name="model">Filtering criteria (user, dates, status).</param>
    /// <param name="includeProducts">If true, performs an eager load of the associated product items.</param>
    /// <returns>A list of orders matching the administrative filters.</returns>
    Task<List<Order>> GetAdminOrderHistoryAsync(ReportOrderSearchViewModel model, bool includeProducts = false);

    /// <summary>
    /// Retrieves the financial transaction history (top-ups and spending) for a user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="model">Filtering criteria (date range, transaction type).</param>
    /// <returns>A list of balance-related transactions.</returns>
    Task<List<Transaction>> GetTransactionHistoryAsync(string userId, ReportTransactionSearchViewModel model);

    #endregion
}