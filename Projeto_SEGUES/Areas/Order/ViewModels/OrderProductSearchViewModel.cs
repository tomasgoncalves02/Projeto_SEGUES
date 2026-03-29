using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Areas.Order.ViewModels;

/// <summary>
/// ViewModel used for searching and filtering products in the order process.
/// </summary>
public class OrderProductSearchViewModel
{
    /// <summary>
    /// The search string entered by the user to filter products.
    /// </summary>
    [Display(Name = "Procurar")]
    public string? SearchString { get; set; }
    
    /// <summary>
    /// The category ID selected by the user to filter products.
    /// </summary>
    [Display(Name = "Categoria")]
    public int? CategoryId { get; set; }
    
    /// <summary>
    /// The collection of order product entities that match the specified search and filter criteria.
    /// </summary>
    public List<OrderProductDto> Results { get; set; } = [];
}