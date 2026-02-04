using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Enums;

public enum DiscountType
{
    [Display(Name = "Porcentagem")]
    Percentage,
    [Display(Name = "Valor Fixo")]
    Fixed
}