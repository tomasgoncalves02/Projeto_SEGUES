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

    /// <summary>
    /// Initializes a new instance of the controller with inventory, logging, and localization services.
    /// </summary>
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
    public async Task<IActionResult> GetProducts([Bind(Prefix = "SearchModel")] InventorySearchViewModel model)
    {
        var rawProducts = await _inventoryService.GetFilteredProductsAsync(model);
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
    public async Task<IActionResult> Create([Bind(Prefix = "NewProduct")] CreateProductViewModel createProductViewModel)
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
        var product = await _inventoryService.GetProductByIdAsync(id);
        if (product == null)
        {
            TempData.SetSwalError("Produto não encontrado.");
            return RedirectToAction(nameof(Index));
        }

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
        
        var result = await _inventoryService.EditProductAsync(createProductViewModel);
        if (result.Success)
        {
            TempData.SetSwalSuccess(result.Message);
            return RedirectToAction(nameof(Index));
        }
        
        TempData.SetSwalError(result.Message);
        return View(createProductViewModel);
    }

    /// <summary>
    /// Removes a product from the system.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    { 
        var result = await _inventoryService.DeleteProductAsync(id);
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
    /// Reactivates a previously disabled product.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reactivate(int id)
    {
        var result = await _inventoryService.ReactivateProductAsync(id);
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
}