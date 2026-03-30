using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Enums;

/// <summary>
/// Defines the mathematical logic used to calculate a price reduction or promotional value.
/// </summary>
/// <remarks>
/// This enum allows the system to differentiate between relative (%) and absolute (€) discounts, 
/// ensuring that the <c>OrderService</c> applies the correct formula during checkout.
/// Inherits from <see cref="byte"/> to minimize database storage footprint.
/// </remarks>
public enum DiscountType : byte
{
    /// <summary>
    /// A relative reduction based on a percentage of the total amount.
    /// Example: 10% off the total order value.
    /// </summary>
    [Display(Name = "Porcentagem")]
    Percentage,

    /// <summary>
    /// An absolute reduction of a specific currency amount.
    /// Example: A €5,00 voucher applied to the order.
    /// </summary>
    [Display(Name = "Valor Fixo")]
    Fixed
}