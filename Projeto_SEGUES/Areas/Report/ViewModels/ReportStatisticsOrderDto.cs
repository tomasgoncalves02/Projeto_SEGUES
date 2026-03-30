namespace Projeto_SEGUES.Areas.Report.ViewModels;

/// <summary>
/// Data Transfer Object (DTO) that aggregates all high-level order statistics and KPIs.
/// </summary>
/// <remarks>
/// This model serves as the primary data source for the administrative dashboard, 
/// combining financial totals, buyer demographics, and structured data for various charts.
/// </remarks>
public class ReportStatisticsOrderDto
{
    /// <summary>Total count of orders processed within the selected report period.</summary>
    public int TotalOrders { get; set; }

    /// <summary>Total gross revenue generated from all orders.</summary>
    public decimal TotalRevenue { get; set; }

    /// <summary>Gross revenue formatted as a localized currency string.</summary>
    public string FormattedTotalRevenue => TotalRevenue.ToString("C");

    /// <summary>Average monetary value per order (Total Revenue / Total Orders).</summary>
    public decimal AverageRevenue { get; set; }

    /// <summary>Average order value formatted as a localized currency string.</summary>
    public string FormattedAverageRevenue => AverageRevenue.ToString("C");

    /// <summary>Total number of unique users who have placed at least one order.</summary>
    public int NumberOfBuyers { get; set; }

    /// <summary>Time-series data used to render the order frequency chart (e.g., Orders per Day).</summary>
    public List<ChartDataDto> OrderChart { get; set; } = [];

    /// <summary>Distribution data used to render a breakdown of sales by product category.</summary>
    public List<CategoryDataDto> ProductCategories { get; set; } = [];

    /// <summary>List of the most frequently purchased products for "Top Seller" rankings.</summary>
    public List<ProductDataDto> TopProducts { get; set; } = [];
}