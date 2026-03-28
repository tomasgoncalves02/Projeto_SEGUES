using Projeto_SEGUES.Models.Order;
using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Inventory;

/// <summary>
/// Entity representing a physical item or service available in the campus inventory.
/// </summary>
/// <remarks>
/// This model handles the core data for products, including pricing, stock thresholds, 
/// and categorization. It maintains a relationship with <see cref="OrderLine"/> to track sales history.
/// </remarks>
public class Product
{
    /// <summary>Unique identifier for the product.</summary>
    public int Id { get; set; }

    /// <summary>The display name of the product (e.g., "Sandes de Delícias").</summary>
    [Required]
    [MaxLength(100, ErrorMessage = "Nome deve ter no máximo {1} caracteres.")]
    [Display(Name = "Nome")]
    public required string Name { get; set; }

    /// <summary>A brief description providing details about the product ingredients or features.</summary>
    [Required]
    [MaxLength(250, ErrorMessage = "Descrição deve ter no máximo {1} caracteres.")]
    [Display(Name = "Descrição")]
    public required string Description { get; set; }

    /// <summary>Navigation property to the category this product belongs to (e.g., Cafetaria, Refeição).</summary>
    [Required]
    [Display(Name = "Categoria")]
    public required ProductCategory Category { get; set; } // FK

    /// <summary>The current unit price of the product.</summary>
    [Required]
    [Range(0, double.MaxValue)]
    [Display(Name = "Preço")]
    public required decimal Price { get; set; }

    /// <summary>The physical quantity currently available in the warehouse/bar.</summary>
    [Required]
    [Range(0, int.MaxValue)]
    [Display(Name = "Stock")]
    public required int Stock { get; set; }

    /// <summary>The critical threshold used to trigger 'Low Stock' alerts in the administration dashboard.</summary>
    [Required]
    [Range(0, int.MaxValue)]
    [Display(Name = "Stock mínimo")]
    public required int MinimumStock { get; set; }

    /// <summary>Toggle for soft-deletion; inactive products are hidden from the user store but kept for historical audits.</summary>
    [Display(Name = "Ativo")]
    public bool IsActive { get; set; } = true;

    /// <summary>Collection of order lines where this product has been purchased.</summary>
    public ICollection<OrderLine> ProductPurchases { get; set; } = new List<OrderLine>();
}