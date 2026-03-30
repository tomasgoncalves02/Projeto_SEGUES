using Projeto_SEGUES.Models.Inventory;
using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Order;

/// <summary>
/// Entity representing a specific line item within a customer order.
/// </summary>
/// <remarks>
/// This model acts as a junction between <see cref="Order"/> and <see cref="Product"/>, 
/// implementing the "Snapshot Pattern" to preserve the unit price and discount state 
/// at the exact moment of transaction.
/// </remarks>
public class OrderLine
{
    /// <summary>Foreign key identifier for the purchased product.</summary>
    [Required]
    public required int ProductId { get; init; }

    /// <summary>Navigation property to the associated product details.</summary>
    [Required]
    public required Product Product { get; init; } // FK

    /// <summary>Foreign key identifier for the parent order.</summary>
    [Required]
    public required int OrderId { get; init; }

    /// <summary>Navigation property to the parent order record.</summary>
    [Required]
    public required Order Order { get; init; } // FK

    /// <summary>The number of units purchased for this specific line item.</summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "A quantidade deve ser maior que zero.")]
    [Display(Name = "Quantidade")]
    public required int Quantity { get; set; } = 1;

    /// <summary>The specific discount rule applied to this item at checkout, if applicable.</summary>
    public Discount? Discount { get; init; }

    /// <summary>
    /// The unit price of the product at the time of purchase.
    /// </summary>
    /// <remarks>
    /// This is a critical audit field; it ensures that future price changes in the 
    /// <see cref="Product"/> table do not retroactively alter the value of past orders.
    /// </remarks>
    [Required]
    [Range(0, double.MaxValue)]
    [Display(Name = "Valor")]
    [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
    public required decimal ProductValue { get; set; } // Value at the time of purchase
}