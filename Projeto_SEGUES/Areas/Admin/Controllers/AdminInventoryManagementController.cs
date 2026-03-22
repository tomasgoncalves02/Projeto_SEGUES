using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Projeto_SEGUES.Areas.Inventory.ViewModels;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Resources;
using Projeto_SEGUES.Services;
using System.Diagnostics;

namespace Projeto_SEGUES.Areas.Admin;

/// <summary>
/// Controller responsible for managing product inventory within the administrative area.
/// </summary>
/// <remarks>
/// This controller handles CRUD operations for products, stock management, and categories,
/// ensuring all actions are logged and exceptions are redirected to a global error page.
/// </remarks>
[Authorize(Roles = "Admin")]
[Area("Admin")]
public class AdminInventoryManagementController : Controller
{
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<AdminInventoryManagementController> _logger;
    private readonly IStringLocalizer<Errors> _localizer;

    /// <summary>
    /// Initializes a new instance of the controller with inventory, logging, and localization services.
    /// </summary>
    public AdminInventoryManagementController(
        IInventoryService inventoryService,
        ILogger<AdminInventoryManagementController> logger,
        IStringLocalizer<Errors> localizer)
    {
        _inventoryService = inventoryService;
        _logger = logger;
        _localizer = localizer;
    }

    /// <summary>
    /// Lists all products and categories available in the system.
    /// </summary>
    /// <returns>The index View populated with current categories and products.</returns>
    public async Task<IActionResult> Index()
    {
        try
        {
            ViewBag.Categories = await _inventoryService.GetAllCategoriesForDropdownAsync();
            ViewBag.Products = await _inventoryService.GetAllProductsAsync();
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogAppError($"Error loading inventory index: {ex.Message}", TableName.Product, AppOperation.Read);
            return RedirectToAction("Error", "Home", new { area = "", errorCode = (int)AppErrors.DatabaseQueryError });
        }
    }

    /// <summary>
    /// Retrieves the product list formatted for a partial interface update via AJAX.
    /// </summary>
    /// <returns>A PartialView containing the products table.</returns>
    public async Task<IActionResult> GetProducts()
    {
        try
        {
            var products = await _inventoryService.GetAllProductsAsync();
            return PartialView("_ProductListPartial", products);
        }
        catch (Exception ex)
        {
            _logger.LogAppError($"Error retrieving partial product list: {ex.Message}", TableName.Product, AppOperation.Read);
            return StatusCode(500); // Internal Server Error for AJAX requests
        }
    }

    /// <summary>
    /// Processes the registration of a new product in the inventory.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductViewModel productViewModel)
    {
        if (!ModelState.IsValid)
        {
            TempData.SetSwalError("Could not register product. Please verify the input fields.");
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var result = await _inventoryService.CreateProductAsync(productViewModel);
            if (result.Success)
            {
                TempData.SetSwalSuccess(result.Message);
            }
            else
            {
                TempData.SetSwalError(result.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogAppError($"Critical error creating product ({productViewModel.Name}): {ex.Message}", TableName.Product, AppOperation.Create);
            return RedirectToAction("Error", "Home", new { area = "", errorCode = (int)AppErrors.ProductCreateErrror });
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Displays the edit form for a specific product.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        try
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
        catch (Exception ex)
        {
            _logger.LogAppError($"Error fetching product ID {id} for edit: {ex.Message}", TableName.Product, AppOperation.Read);
            return RedirectToAction("Error", "Home", new { area = "", errorCode = (int)AppErrors.DatabaseQueryError });
        }
    }

    /// <summary>
    /// Processes the changes made to an existing product.
    /// </summary>
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

        try
        {
            var result = await _inventoryService.EditProductAsync(productViewModel);
            if (result.Success)
            {
                TempData.SetSwalSuccess(result.Message);
            }
            else
            {
                TempData.SetSwalError(result.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogAppError($"Critical error updating product ID {productViewModel.Id}: {ex.Message}", TableName.Product, AppOperation.Update);
            return RedirectToAction("Error", "Home", new { area = "", errorCode = (int)AppErrors.ProductEditError });
        }

        return View(productViewModel);
    }

    /// <summary>
    /// Removes a product from the system.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
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
        }
        catch (Exception ex)
        {
            _logger.LogAppError($"Critical error deleting product ID {id}: {ex.Message}", TableName.Product, AppOperation.Delete);
            return RedirectToAction("Error", "Home", new { area = "", errorCode = (int)AppErrors.ProductDeleteError });
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Reactivates a previously disabled product.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reactivate(int id)
    {
        try
        {
            var result = await _inventoryService.ReactivateProductAsync(id);
            if (!result.Success)
            {
                TempData.SetSwalError(result.Message);
            }
            else
            {
                TempData.SetSwalSuccess(result.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogAppError($"Critical error reactivating product ID {id}: {ex.Message}", TableName.Product, AppOperation.Update);
            return RedirectToAction("Error", "Home", new { area = "", errorCode = (int)AppErrors.DatabaseUpdateError });
        }

        return RedirectToAction(nameof(Index));
    }
}