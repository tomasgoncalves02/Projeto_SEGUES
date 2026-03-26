namespace Projeto_SEGUES.Areas.Report.ViewModels;

/// <summary>
/// Data Transfer Object (DTO) used for representing statistical data at the product level.
/// </summary>
/// <remarks>
/// This model is primarily used for ranking and performance reports, such as identifying 
/// top-selling items or products with the highest turnover within the system.
/// </remarks>
public class ProductDataDto
{
    /// <summary>
    /// The unique commercial name of the product.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// The total quantity associated with the product in a given report context.
    /// </summary>
    /// <remarks>
    /// Depending on the query, this value typically represents the total units sold 
    /// or the current total volume available in the inventory.
    /// </remarks>
    public int Quantity { get; set; }
}