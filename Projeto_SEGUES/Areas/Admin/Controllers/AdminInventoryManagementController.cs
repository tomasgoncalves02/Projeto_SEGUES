using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Areas.Inventory.ViewModels;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Services;

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

    /// <summary>
    /// Initializes a new instance of the controller with inventory, logging, and localization services.
    /// </summary>
    public AdminInventoryManagementController(
        IInventoryService inventoryService,
        ILogger<AdminInventoryManagementController> logger)
    {
        _inventoryService = inventoryService;
        _logger = logger;
    }

    /// <summary>
    /// Lists all products and categories available in the system.
    /// </summary>
    /// <returns>The index View populated with current categories and products.</returns>
    public async Task<IActionResult> Index()
    {
        var rawProducts = await _inventoryService.GetAllProductsAsync();
        InventoryManagementViewModel vm = new InventoryManagementViewModel
        {
            Categories = await _inventoryService.GetAllCategoriesForDropdownAsync(),
            Products = rawProducts.Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                CategoryId = p.Category.Id,
                CategoryName = p.Category.Name,
                Price = p.Price,
                Stock = p.Stock,
                MinimumStock = p.MinimumStock,
                IsActive = p.IsActive,
                ModalInfo = new
                {
                    name = p.Name,
                    description = p.Description,
                    price = p.Price.ToString("C2"),
                    categoryName = p.Category.Name,
                    categoryDescription = p.Category.Description,
                    stock = p.Stock,
                    minStock = p.MinimumStock
                }
            }).ToList()
        };
        return View(vm);
    }

    /// <summary>
    /// Retrieves the product list formatted for a partial interface update via AJAX.
    /// </summary>
    /// <returns>A PartialView containing the products table.</returns>
    public async Task<IActionResult> GetProducts()
    {
        var rawProducts = await _inventoryService.GetAllProductsAsync();
        List<ProductDto> products = rawProducts.Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            CategoryId = p.Category.Id,
            CategoryName = p.Category.Name,
            Price = p.Price,
            Stock = p.Stock,
            MinimumStock = p.MinimumStock,
            IsActive = p.IsActive,
            ModalInfo = new
            {
                name = p.Name,
                description = p.Description,
                price = p.Price.ToString("C2"),
                categoryName = p.Category.Name,
                categoryDescription = p.Category.Description,
                stock = p.Stock,
                minStock = p.MinimumStock
            }
        }).ToList();
        return PartialView("_ProductListPartial", products);
    }

    /// <summary>
    /// Processes the registration of a new product in the inventory.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateProductViewModel createProductViewModel)
    {
        if (!ModelState.IsValid)
        {
            TempData.SetSwalError("Não foi possível registar o produto. Verifique os campos.");
            return RedirectToAction(nameof(Index));
        }

        var result = await _inventoryService.CreateProductAsync(createProductViewModel);
        if (result.Success)
        {
            TempData.SetSwalSuccess(result.Message);
        }
        else
        {
            return RedirectToAction("Error", "Home", new { area = "", errorCode = AppErrors.ProductCreateError });
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

            CreateProductViewModel createProductViewModel = new CreateProductViewModel
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
            return View(createProductViewModel);
        }
        catch (Exception ex)
        {
            _logger.LogAppError(AppErrors.DatabaseQueryError, TableName.Product, AppOperation.Read, ex);
            return RedirectToAction("Error", "Home", new { area = "", errorCode = AppErrors.DatabaseQueryError });
        }
    }

    /// <summary>
    /// Processes the changes made to an existing product.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CreateProductViewModel createProductViewModel)
    {
        ViewBag.Categories = await _inventoryService.GetAllCategoriesForDropdownAsync();

        if (!ModelState.IsValid)
        {
            TempData.SetSwalError("Não foi possível atualizar o produto. Verifique os campos.");
            return View(createProductViewModel);
        }

        try
        {
            var result = await _inventoryService.EditProductAsync(createProductViewModel);
            if (result.Success)
            {
                TempData.SetSwalSuccess(result.Message);
                return RedirectToAction(nameof(Index));
            }
            else
            {
                TempData.SetSwalError(result.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogAppError(AppErrors.ProductEditError, TableName.Product, AppOperation.Update, ex);
            return RedirectToAction("Error", "Home", new { area = "", errorCode = AppErrors.ProductEditError });
        }

        return View(createProductViewModel);
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
            _logger.LogAppError(AppErrors.ProductDeleteError, TableName.Product, AppOperation.Update, ex);
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
            _logger.LogAppError(AppErrors.DatabaseUpdateError, TableName.Product, AppOperation.Update);
            return RedirectToAction("Error", "Home", new { area = "", errorCode = AppErrors.DatabaseUpdateError });
        }

        return RedirectToAction(nameof(Index));
    }
}