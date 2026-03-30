namespace Projeto_SEGUES.Areas.Order.ViewModels;

/// <summary>
/// Data Transfer Object (DTO) representing a single product within an order context.
/// </summary>
/// <remarks>
/// This class encapsulates product details, quantities, and monetary calculations, 
/// serving as the primary bridge between the inventory data and the ordering interface.
/// </remarks>
public class OrderProductDto
{
    /// <summary>Unique identifier of the product.</summary>
    public int Id { get; set; }

    /// <summary>The display name of the product.</summary>
    public string Name { get; set; } = "";

    /// <summary>The unit price of the product.</summary>
    public decimal Price { get; set; }

    /// <summary>Unit price formatted as a currency string.</summary>
    public string FormattedPrice => Price.ToString("C");

    /// <summary>The quantity of this specific product included in the order.</summary>
    public int Quantity { get; set; }

    /// <summary>Calculated subtotal for this item (Price * Quantity).</summary>
    private decimal TotalPrice => Price * Quantity;

    /// <summary>Subtotal formatted as a currency string.</summary>
    public string FormattedTotalPrice => TotalPrice.ToString("C");

    /// <summary>The foreign key of the associated product category.</summary>
    public int CategoryId { get; set; }

    /// <summary>The display name of the category (e.g., "Meals", "Drinks").</summary>
    public string CategoryName { get; set; } = "";

    /// <summary>
    /// Anonymous object used to pass structured data to JavaScript components.
    /// </summary>
    /// <remarks>
    /// This property is typically used to populate client-side modals or 
    /// dynamic UI updates without extra API calls.
    /// </remarks>
    public object ModalInfo { get; set; } = null!;
}