namespace Projeto_SEGUES.Areas.Report.ViewModels;

public class ReportStatisticsOrderDto
{
    public int TotalOrders { get; set; }
    
    public decimal TotalRevenue { get; set; }
    public string FormattedTotalRevenue => TotalRevenue.ToString("C");

    public decimal AverageRevenue { get; set; }
    public string FormattedAverageRevenue => AverageRevenue.ToString("C");

    public int NumberOfBuyers { get; set; }
    
    public List<ChartDataDto> OrderChart { get; set; } = [];
    public List<CategoryDataDto> ProductCategories { get; set; } = [];
    public List<ProductDataDto> TopProducts { get; set; } = [];
}