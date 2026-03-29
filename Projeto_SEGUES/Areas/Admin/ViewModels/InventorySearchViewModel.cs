using System.ComponentModel.DataAnnotations;
using Projeto_SEGUES.Models.Enums;

namespace Projeto_SEGUES.Areas.Admin.ViewModels;

/// <summary>
/// ViewModel used to capture user-defined filters for searching and auditing products in the inventory.
/// </summary>
/// <remarks>
/// This model supports multi-criteria filtering, allowing administrators to narrow down 
/// the product list by name, category, price thresholds, and stock availability.
/// </remarks>
public class InventorySearchViewModel
{
    /// <summary>
    /// Text-based search string used to filter products by Name or Description.
    /// </summary>
    [Display(Name = "Pesquisar")]
    public string? SearchString { get; set; }

    /// <summary>
    /// Foreign key identifier for filtering products by a specific category.
    /// </summary>
    [Display(Name = "Categoria")]
    [Range(0, int.MaxValue)]
    public int? CategoryId { get; set; }

    /// <summary>
    /// Upper limit for the unit price filter.
    /// </summary>
    [Display(Name = "Preço Máximo")]
    [Range(0, double.MaxValue)]
    public decimal? MaxPrice { get; set; }

    /// <summary>
    /// Filter based on the stock quantity status (e.g., InStock, LowStock, OutOfStock).
    /// </summary>
    [Display(Name = "Nível de Stock")]
    public StockLevel? StockLevel { get; set; }

    /// <summary>
    /// Toggle to exclude products that have been soft-deleted or marked as inactive.
    /// </summary>
    [Display(Name = "Estado")]
    public bool ActiveOnly { get; set; }
}