using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Areas.Order.ViewModels;

public class OrderTotalViewModel
{
    [Range(0, int.MaxValue)]
    [Display(Name = "Quantidade Total")]
    public int TotalQuantity { get; set; } = 0;
    
    [Range(0, double.MaxValue)]
    [Display(Name = "Valor Total")]
    public decimal TotalValue { get; set; } = 0m;
}