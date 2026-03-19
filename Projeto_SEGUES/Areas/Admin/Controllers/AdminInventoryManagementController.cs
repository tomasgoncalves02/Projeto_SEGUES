using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Areas.Inventory.ViewModels;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Admin;

/// <summary>
/// Controller responsible for managing product inventory within the administrative area.
/// </summary>
/// <remarks>
/// Allows administrators to perform CRUD operations (Create, Read, Update, Delete) on products, 
/// as well as manage stock levels and associated categories.
/// </remarks>
[Authorize(Roles = "Admin")]
[Area("Admin")]
public class AdminInventoryManagementController : Controller
{
    private readonly IInventoryService _inventoryService;

    /// <summary>
    /// Initializes a new instance of the controller with the inventory service.
    /// </summary>
    /// <param name="inventoryService">Service interface containing the inventory business logic.</param>
    public AdminInventoryManagementController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    /// <summary>
    /// Lists all products and categories available in the system.
    /// </summary>
    /// <returns>The index View populated with current categories and products.</returns>
    public async Task<IActionResult> Index()
    {
        ViewBag.Categories = await _inventoryService.GetAllCategoriesForDropdownAsync();
        ViewBag.Products = await _inventoryService.GetAllProductsAsync();
        return View();
    }

    /// <summary>
    /// Retrieves the product list formatted for a partial interface update.
    /// </summary>
    /// <returns>A PartialView containing the products table or list.</returns>
    public async Task<IActionResult> GetProducts()
    {
        var products = await _inventoryService.GetAllProductsAsync();
        return PartialView("_ProductListPartial", products);
    }

    /// <summary>
    /// Processes the registration of a new product in the inventory.
    /// </summary>
    /// <param name="productViewModel">Data model containing the details of the product to be created.</param>
    /// <returns>Redirects to Index with a success or error message via SweetAlert.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductViewModel productViewModel)
    {
        if (!ModelState.IsValid)
        {
            TempData.SetSwalError($"Não foi possível registar o produto. Verifique os campos.");
            return RedirectToAction(nameof(Index));
        }
        var result = await _inventoryService.CreateProductAsync(productViewModel);
        if (result.Success)
        {
            TempData.SetSwalSuccess(result.Message);
        }
        else
        {
            TempData.SetSwalError(result.Message);
        }
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Removes a product from the system based on the provided identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the product.</param>
    /// <returns>Redirects to Index reporting the result of the operation.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _inventoryService.DeleteProductAsync(id);
        if (!result.Success)
        {
            TempData.SetSwalError(result.Message);
        }
        else
        {
            TempData.SetSwalSuccess(result.Message);
        }
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Displays the edit form for a specific product.
    /// </summary>
    /// <param name="id">The unique identifier of the product to edit.</param>
    /// <returns>The edit View filled with current data or NotFound if the product does not exist.</returns>
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await _inventoryService.GetProductByIdAsync(id);
        if (product == null) return NotFound();
        ViewBag.Categories = await _inventoryService.GetAllCategoriesForDropdownAsync();
        ProductViewModel productViewModel = new ProductViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            CategoryId = product.Category.Id,
            Price = product.Price,
            Stock = product.Stock,
            MinimumStock = product.MinimumStock,
            IsActive = product.IsActive
        };
        return View(productViewModel);
    }

    /// <summary>
    /// Processes the changes made to an existing product.
    /// </summary>
    /// <param name="productViewModel">Data model with the updated information.</param>
    /// <returns>The same edit View with the result of the operation.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProductViewModel productViewModel)
    {
        ViewBag.Categories = await _inventoryService.GetAllCategoriesForDropdownAsync();
        if (!ModelState.IsValid)
        {
            TempData.SetSwalError("Não foi possível atualizar o produto. Verifique os campos.");
            return View(productViewModel);
        }
        var result = await _inventoryService.EditProductAsync(productViewModel);
        if (!result.Success)
        {
            TempData.SetSwalError(result.Message);
        }
        else
        {
            TempData.SetSwalSuccess(result.Message);
        }
        return View(productViewModel);
    }

    /// <summary>
    /// Reactivates a previously disabled product.
    /// </summary>
    /// <param name="id">The unique identifier of the product.</param>
    /// <returns>Redirects to Index reporting the result.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reactivate(int id)
    {
        // O serviço deve ter uma lógica para definir IsActive = true
        var result = await _inventoryService.ReactivateProductAsync(id);

        if (!result.Success)
        {
            TempData.SetSwalError(result.Message);
        }
        else
        {
            TempData.SetSwalSuccess(result.Message);
        }
        return RedirectToAction(nameof(Index));
    }
}