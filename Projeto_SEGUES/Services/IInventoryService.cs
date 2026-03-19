using Microsoft.AspNetCore.Mvc.Rendering;
using Projeto_SEGUES.Areas.Inventory.ViewModels;
using Projeto_SEGUES.Models.Inventory;

namespace Projeto_SEGUES.Services;

public interface IInventoryService
{
    Task<Product?> GetProductByIdAsync(int id);
    Task<List<Product>> GetAvailableProductsAsync();
    Task<List<Product>> GetAllProductsAsync();
    Task<List<SelectListItem>> GetAllCategoriesForDropdownAsync();
    Task<ServiceResult> CreateProductAsync(ProductViewModel productViewModel);
    Task<ServiceResult> EditProductAsync(ProductViewModel productViewModel);
    Task<ServiceResult> DeleteProductAsync(int id);
    Task<ServiceResult> ReactivateProductAsync(int id);
}