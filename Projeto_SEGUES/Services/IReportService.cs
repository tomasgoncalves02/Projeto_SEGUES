using Projeto_SEGUES.Areas.Report.ViewModels;
using Projeto_SEGUES.Models.Order;
using Projeto_SEGUES.Models.Payment;

namespace Projeto_SEGUES.Services;

public interface IReportService
{
    // Statistics
    Task<ReportStatisticsTicketDto> GetTicketsStats(int period = 1);
    Task<ReportStatisticsOrderDto> GetOrdersStats(int period = 1);
    // History
    Task<List<Order>> GetOrderHistoryAsync(string userId, ReportOrderSearchViewModel model);
    Task<List<Order>> GetAdminOrderHistoryAsync(ReportOrderSearchViewModel model, bool includeProducts = false);
    Task<List<Transaction>> GetTransactionHistoryAsync(string userId, ReportTransactionSearchViewModel model);
}