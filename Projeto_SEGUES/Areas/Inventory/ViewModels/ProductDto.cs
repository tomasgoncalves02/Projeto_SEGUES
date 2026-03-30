namespace Projeto_SEGUES.Areas.Inventory.ViewModels;

/// <summary>
/// Data Transfer Object (DTO) for product information within the inventory.
/// </summary>
/// <remarks>
/// This DTO is used to transport product data between the service layer and the View, 
/// containing calculated properties for UI styling and status representation.
/// </remarks>
public class ProductDto
{
    /// <summary>Unique identifier of the product.</summary>
    public int Id { get; set; }

    /// <summary>Commercial name of the product.</summary>
    public string Name { get; set; } = "";

    /// <summary>Name of the associated category (e.g., Beverages, Snacks).</summary>
    public string CategoryName { get; set; } = "";

    /// <summary>Foreign key for the product category.</summary>
    public int CategoryId { get; set; }

    /// <summary>Unit price of the product.</summary>
    public decimal Price { get; set; }

    /// <summary>Price formatted as a currency string based on the system's culture.</summary>
    public string FormattedPrice => Price.ToString("C");

    /// <summary>Current quantity available in stock.</summary>
    public int Stock { get; set; }

    /// <summary>Threshold value that triggers a low-stock warning.</summary>
    public int MinimumStock { get; set; }

    /// <summary>Indicates if the product is available for sale.</summary>
    public bool IsActive { get; set; }

    /// <summary>Returns the CSS class for styling inactive product rows in the table.</summary>
    public string InactiveRowClass => IsActive ? "" : "text-muted text-decoration-line-through";

    /// <summary>Returns the Bootstrap badge class based on the current stock level vs. minimum stock.</summary>
    public string StockBadgeClass => Stock <= 0 ? "bg-danger" : (Stock < MinimumStock ? "bg-warning" : "bg-success");

    /// <summary>Data structure used to populate the details or edit modal via JSON.</summary>
    public object ModalInfo { get; set; } = null!;
}