using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using Projeto_SEGUES.Areas.Inventory.ViewModels;

namespace Projeto_SEGUES.Areas.Admin.ViewModels;

/// <summary>
/// ViewModel used for the administrative management of the shop's inventory and product catalog.
/// </summary>
/// <remarks>
/// This model acts as a container for bulk data display (Product List), 
/// dynamic UI elements (Categories dropdown), and nested operations (Search and Creation).
/// </remarks>
public class InventoryManagementViewModel
{
    /// <summary>
    /// A collection of Data Transfer Objects (DTOs) representing the current state of products in the inventory.
    /// </summary>
    /// <remarks>
    /// Using [ValidateNever] to skip validation during post-back, as this list is read-only 
    /// for display purposes within the management dashboard.
    /// </remarks>
    [ValidateNever]
    public List<ProductDto> Products { get; set; } = new();

    /// <summary>
    /// List of product categories formatted for an HTML select (dropdown) element.
    /// </summary>
    [ValidateNever]
    public List<SelectListItem> Categories { get; set; } = new();

    /// <summary>
    /// Nested ViewModel dedicated to filtering and searching the inventory.
    /// </summary>
    public InventorySearchViewModel SearchModel { get; set; } = new();

    /// <summary>
    /// Nested ViewModel used to capture data for registering a new product.
    /// </summary>
    /// <remarks>
    /// This property isolates the "Create" form data from the "Management" display data, 
    /// following the Single Responsibility Principle within the View.
    /// </remarks>
    public CreateProductViewModel NewProduct { get; set; } = new()
    {
        Name = "",
        Description = "",
        CategoryId = 0,
        Price = 0,
        Stock = 0,
        MinimumStock = 0
    };
}