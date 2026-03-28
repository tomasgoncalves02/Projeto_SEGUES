using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using Projeto_SEGUES.Areas.Inventory.ViewModels;

namespace Projeto_SEGUES.Areas.Admin.ViewModels;

public class InventoryManagementViewModel
{
    [ValidateNever]
    public List<ProductDto> Products { get; set; } = new();
    [ValidateNever]
    public List<SelectListItem> Categories { get; set; } = new();
    
    // Search model
    public InventorySearchViewModel SearchModel { get; set; } = new();
    
    // For new product registration form
    public CreateProductViewModel NewProduct { get; set; } = new() { Name = "", Description = "", CategoryId = 0, Price = 0, Stock = 0, MinimumStock = 0 };
}