using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Areas.Payment.ViewModels;

public class DepositAmountViewModel
{
    [Required(ErrorMessage = "O montante é obrigatório.")]
    [Range(5, 1000, ErrorMessage = "O montante deve ser entre 5€ e 1000€.")]
    [Display(Name = "Montante (€)")]
    public decimal Amount { get; set; }
}