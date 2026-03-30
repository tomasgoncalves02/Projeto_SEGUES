using Projeto_SEGUES.Areas.Admin.ViewModels;
using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Inventory;

/// <summary>
/// Entity representing a logical grouping for products (e.g., "Bar", "Refeitório", "Papelaria").
/// </summary>
/// <remarks>
/// This model is essential for the <see cref="InventoryManagementViewModel"/> as it allows 
/// administrators to filter stock and sales reports by specific business departments.
/// </remarks>
public class ProductCategory
{
    /// <summary>Unique identifier for the product category.</summary>
    public int Id { get; init; }

    /// <summary>The display name of the category shown in menus and filters.</summary>
    [Required]
    [MaxLength(100, ErrorMessage = "Nome deve ter no máximo {1} caracteres.")]
    [Display(Name = "Nome")]
    public required string Name { get; init; }

    /// <summary>A brief overview of what types of products are included in this category.</summary>
    [Required]
    [MaxLength(250, ErrorMessage = "Descrição deve ter no máximo {1} caracteres.")]
    [Display(Name = "Descrição")]
    public required string Description { get; init; }
}