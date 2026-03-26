using Microsoft.AspNetCore.Mvc.Rendering;
using Projeto_SEGUES.Areas.Inventory.ViewModels;

namespace Projeto_SEGUES.Areas.Admin.ViewModels;

public class InventoryManagementViewModel
{
    public List<ProductDto> Products { get; set; } = new();
    public List<SelectListItem> Categories { get; set; } = new();
    
    // For new product registration form
    public CreateProductViewModel NewProduct { get; set; } = new() { Name = "", Description = "", CategoryId = 0, Price = 0, Stock = 0, MinimumStock = 0 };
}