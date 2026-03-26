using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Areas.Payment.ViewModels;

/// <summary>
/// ViewModel used for defining the amount during a balance deposit operation.
/// </summary>
/// <remarks>
/// This model ensures that the user inputs a monetary value within the business-defined 
/// limits (minimum 5€, maximum 1000€) before proceeding to the payment gateway selection.
/// </remarks>
public class DepositAmountViewModel
{
    /// <summary>
    /// Gets or sets the monetary amount to be deposited.
    /// </summary>
    /// <value>Required value between 5.00 and 1000.00.</value>
    [Required(ErrorMessage = "O montante é obrigatório.")]
    [Range(5, 1000, ErrorMessage = "O montante deve ser entre 5€ e 1000€.")]
    [Display(Name = "Montante (€)")]
    public decimal Amount { get; set; }
}