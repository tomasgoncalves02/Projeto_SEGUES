using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Areas.Inventory.ViewModels;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Inventory;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Admin;

[Authorize(Roles = "Admin")]
[Area("Admin")]
public class AdminInventoryManagementController : Controller
{
    private readonly IInventoryService _inventoryService;
    
    public AdminInventoryManagementController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }
    
    public async Task<IActionResult> Index()
    {
        ViewBag.Categories = await _inventoryService.GetAllCategoriesForDropdownAsync();
        ViewBag.Products = await _inventoryService.GetAllProductsAsync();
        return View();
    }
    
    public async Task<IActionResult> GetProducts()
    {
        var products = await _inventoryService.GetAllProductsAsync();
        return PartialView("_ProductListPartial", products);
    }

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
        if (result.Success) {
            TempData.SetSwalSuccess(result.Message);
        }
        else
        {
            TempData.SetSwalError(result.Message);
        }
        return RedirectToAction(nameof(Index));
    }
    
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
    
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await _inventoryService.GetProductByIdAsync(id);
        if (product == null) return NotFound();
        ViewBag.Categories = await _inventoryService.GetAllCategoriesForDropdownAsync();
        return View(product);
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Product product)
    {
        ViewBag.Categories = await _inventoryService.GetAllCategoriesForDropdownAsync();
        if (!ModelState.IsValid)
        {
            TempData.SetSwalError("Não foi possível atualizar o produto. Verifique os campos.");
            return View(product);
        }
        var result = await _inventoryService.EditProductAsync(product);
        if (!result.Success)
        {
            TempData.SetSwalError(result.Message);
        }
        else
        {
            TempData.SetSwalSuccess(result.Message);
        }
        return View(product);
    }
}