namespace Projeto_SEGUES.Areas.Report.ViewModels;

public class ReportStatisticsTicketDto
{
    public int TotalUsedTickets { get; set; }
    
    public decimal TotalRevenue { get; set; }
    public string FormattedTotalRevenue => TotalRevenue.ToString("C");
    
    public decimal AverageRevenue { get; set; }
    public string FormattedAverageRevenue => AverageRevenue.ToString("C");
    
    public int NumberOfBuyers { get; set; }
    
    public List<ChartDataDto> Chart { get; set; } = [];
    public List<CategoryDataDto> ByCategory { get; set; } = [];
}