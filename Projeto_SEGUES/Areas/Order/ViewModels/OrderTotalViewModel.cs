using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Areas.Order.ViewModels;

/// <summary>
/// ViewModel representing the financial and quantitative summary of an order or cart.
/// </summary>
/// <remarks>
/// This model is frequently used in JSON responses to update UI elements in real-time, 
/// such as the item counter in the header and the accumulated total value.
/// </remarks>
public class OrderTotalViewModel
{
    /// <summary>
    /// Total sum of all individual items present in the order or cart.
    /// </summary>
    /// <value>Non-negative integer value.</value>
    [Range(0, int.MaxValue)]
    [Display(Name = "Quantidade Total")]
    public int TotalQuantity { get; set; }

    /// <summary>
    /// Total monetary value of the order, calculated by summing the unit price multiplied by the quantity of each product.
    /// </summary>
    /// <value>Non-negative decimal value.</value>
    [Range(0, double.MaxValue)]
    [Display(Name = "Valor Total")]
    public decimal TotalValue { get; set; }
}