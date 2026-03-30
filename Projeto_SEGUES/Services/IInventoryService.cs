using Microsoft.AspNetCore.Mvc.Rendering;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Models.Inventory;

namespace Projeto_SEGUES.Services;

/// <summary>
/// Interface for the Inventory Management Service.
/// Defines the core operations for handling the product catalog, stock levels, 
/// and administrative inventory actions.
/// </summary>
public interface IInventoryService
{
    /// <summary>Retrieves a specific product by its unique identifier.</summary>
    /// <param name="id">The product ID.</param>
    /// <returns>The product entity or null if not found.</returns>
    Task<Product?> GetProductByIdAsync(int id);

    /// <summary>Retrieves all products currently available for purchase (active and in stock).</summary>
    /// <returns>A list of active products.</returns>
    Task<List<Product>> GetAvailableProductsAsync();

    /// <summary>Retrieves all products in the database, including inactive ones.</summary>
    /// <returns>A complete list of products.</returns>
    Task<List<Product>> GetAllProductsAsync();

    /// <summary>
    /// Filters the product list based on criteria provided in the search view model.
    /// Used primarily for the administrative inventory dashboard.
    /// </summary>
    /// <param name="model">The search filters (name, category, price, stock level).</param>
    /// <returns>A filtered list of products.</returns>
    Task<List<Product>> GetFilteredProductsAsync(InventorySearchViewModel model);

    /// <summary>Fetches all product categories for population of dropdown menus in forms.</summary>
    /// <returns>A list of SelectListItems representing categories.</returns>
    Task<List<SelectListItem>> GetAllCategoriesForDropdownAsync();

    /// <summary>Creates and persists a new product in the inventory.</summary>
    /// <param name="createProductViewModel">The data for the new product.</param>
    /// <returns>A ServiceResult representing the operation outcome.</returns>
    Task<ServiceResult> CreateProductAsync(CreateProductViewModel createProductViewModel);

    /// <summary>Updates an existing product's information and stock levels.</summary>
    /// <param name="createProductViewModel">The updated data (reuse of creation ViewModel).</param>
    /// <returns>A ServiceResult representing the operation outcome.</returns>
    Task<ServiceResult> EditProductAsync(CreateProductViewModel createProductViewModel);

    /// <summary>Soft deletes a product from the inventory (sets status to inactive).</summary>
    /// <param name="id">The ID of the product to delete.</param>
    /// <returns>A ServiceResult representing the operation outcome.</returns>
    Task<ServiceResult> DeleteProductAsync(int id);

    /// <summary>Reactivates a previously deleted or inactive product.</summary>
    /// <param name="id">The ID of the product to reactivate.</param>
    /// <returns>A ServiceResult representing the operation outcome.</returns>
    Task<ServiceResult> ReactivateProductAsync(int id);
}