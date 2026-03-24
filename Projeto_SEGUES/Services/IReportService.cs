using Projeto_SEGUES.Areas.Report.ViewModels;

namespace Projeto_SEGUES.Services;

public interface IReportService
{
    Task<ReportStatisticsTicketDto> GetTicketsStats(int period = 1);
    Task<ReportStatisticsOrderDto> GetOrdersStats(int period = 1);
}