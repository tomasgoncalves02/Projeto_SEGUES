using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Enums;

public enum StockLevel
{
    [Display(Name = "Com Stock (>0)")]
    InStock,
    
    [Display(Name = "Stock Baixo (< Mínimo)")]
    LowStock,
    
    [Display(Name = "Sem Stock (=0)")]
    OutOfStock
}