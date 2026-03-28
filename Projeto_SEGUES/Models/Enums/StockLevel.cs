using Projeto_SEGUES.Areas.Admin.ViewModels;
using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Enums;

/// <summary>
/// Categorization of product availability based on current inventory quantities.
/// </summary>
/// <remarks>
/// This enum is used primarily by the <see cref="InventorySearchViewModel"/> and 
/// administrative reporting logic to trigger alerts when products need restocking.
/// Unlike <see cref="OrderStatus"/>, this represents a calculated state rather than a static database value.
/// </remarks>
public enum StockLevel
{
    /// <summary>The product is available for purchase and above the minimum threshold.</summary>
    [Display(Name = "Com Stock (>0)")]
    InStock,

    /// <summary>The product is available, but the quantity has fallen below the defined 'MinimumStock' limit.</summary>
    [Display(Name = "Stock Baixo (< Mínimo)")]
    LowStock,

    /// <summary>The product is completely unavailable (Quantity = 0).</summary>
    [Display(Name = "Sem Stock (=0)")]
    OutOfStock
}