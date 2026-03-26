using Microsoft.AspNetCore.Mvc.Rendering;

namespace Projeto_SEGUES.Areas.Order.ViewModels;

/// <summary>
/// ViewModel used for the initial order creation interface (POS/Shopping Cart).
/// </summary>
/// <remarks>
/// This model aggregates the product list, category filters, and total calculations 
/// required to render the ordering screen for the user or employee.
/// </remarks>
public class CreateOrderViewModel
{
    /// <summary>
    /// Collection of products available for selection in the current order context.
    /// </summary>
    public IEnumerable<OrderProductDto> Products { get; set; } = new List<OrderProductDto>();

    /// <summary>
    /// List of categories used to filter the product display in the UI.
    /// </summary>
    /// <value>A collection of <see cref="SelectListItem"/> for dropdown or button group rendering.</value>
    public IEnumerable<SelectListItem> Categories { get; set; } = new List<SelectListItem>();

    /// <summary>
    /// Detailed breakdown of the current cart totals (subtotal, taxes, discounts).
    /// </summary>
    public OrderTotalViewModel CartTotal { get; set; } = new();
}