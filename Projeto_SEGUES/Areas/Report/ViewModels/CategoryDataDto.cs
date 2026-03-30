namespace Projeto_SEGUES.Areas.Report.ViewModels;

/// <summary>
/// Data Transfer Object (DTO) used for representing statistical data grouped by category.
/// </summary>
/// <remarks>
/// This model is primarily designed to be serialized into JSON to feed data visualization 
/// components, such as Chart.js or Google Charts, in the administrative dashboards.
/// </remarks>
public class CategoryDataDto
{
    /// <summary>
    /// The name or label of the category (e.g., "Beverages", "Snacks", "Meals").
    /// </summary>
    public string Category { get; set; } = "";

    /// <summary>
    /// The quantitative value associated with the category.
    /// </summary>
    /// <remarks>
    /// Depending on the report context, this may represent the total number of items sold, 
    /// the number of products registered in the category, or the frequency of orders.
    /// </remarks>
    public int Count { get; set; }
}