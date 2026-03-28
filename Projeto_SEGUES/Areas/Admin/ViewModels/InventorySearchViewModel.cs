using System.ComponentModel.DataAnnotations;
using Projeto_SEGUES.Models.Enums;

namespace Projeto_SEGUES.Areas.Admin.ViewModels;

public class InventorySearchViewModel
{
    [Display(Name = "Pesquisar")]
    public string? SearchString { get; set; }
    
    [Display(Name = "Categoria")]
    [Range(0, int.MaxValue)]
    public int? CategoryId { get; set; }
    
    [Display(Name = "Preço Máximo")]
    [Range(0, double.MaxValue)]
    public decimal? MaxPrice { get; set; }
    
    [Display(Name = "Nível de Stock")]
    public StockLevel? StockLevel { get; set; }
    
    [Display(Name = "Estado")]
    public bool ActiveOnly { get; set; }
}