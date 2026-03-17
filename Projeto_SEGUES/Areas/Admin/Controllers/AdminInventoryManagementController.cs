using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Areas.Inventory.ViewModels;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Inventory;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Admin;

/// <summary>
/// Controller responsável pela gestão do inventário de produtos na área administrativa.
/// </summary>
/// <remarks>
/// Permite aos administradores realizar operações de CRUD (Criar, Ler, Atualizar, Eliminar) em produtos, 
/// além de gerir stocks e categorias associadas.
/// </remarks>
[Authorize(Roles = "Admin")]
[Area("Admin")]
public class AdminInventoryManagementController : Controller
{
    private readonly IInventoryService _inventoryService;

    /// <summary>
    /// Inicializa uma nova instância do controlador com o serviço de inventário.
    /// </summary>
    /// <param name="inventoryService">Interface do serviço que contém a lógica de negócio do inventário.</param>
    public AdminInventoryManagementController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    /// <summary>
    /// Lista todos os produtos e categorias disponíveis no sistema.
    /// </summary>
    /// <returns>A View de índice populada com as categorias e produtos atuais.</returns>
    public async Task<IActionResult> Index()
    {
        ViewBag.Categories = await _inventoryService.GetAllCategoriesForDropdownAsync();
        ViewBag.Products = await _inventoryService.GetAllProductsAsync();
        return View();
    }

    /// <summary>
    /// Obtém a lista de produtos formatada para uma atualização parcial da interface.
    /// </summary>
    /// <returns>Uma PartialView contendo a tabela ou lista de produtos.</returns>
    public async Task<IActionResult> GetProducts()
    {
        var products = await _inventoryService.GetAllProductsAsync();
        return PartialView("_ProductListPartial", products);
    }

    /// <summary>
    /// Processa o registo de um novo produto no inventário.
    /// </summary>
    /// <param name="productViewModel">Modelo de dados contendo os detalhes do produto a criar.</param>
    /// <returns>Redireciona para o Index com uma mensagem de sucesso ou erro via SweetAlert.</returns>
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
    /// Remove um produto do sistema com base no identificador fornecido.
    /// </summary>
    /// <param name="id">Identificador único do produto.</param>
    /// <returns>Redireciona para o Index informando o resultado da operação.</returns>
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
    /// Apresenta o formulário de edição para um produto específico.
    /// </summary>
    /// <param name="id">Identificador único do produto a editar.</param>
    /// <returns>A View de edição preenchida com os dados atuais ou NotFound caso o produto não exista.</returns>
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
    /// Processa as alterações efetuadas num produto existente.
    /// </summary>
    /// <param name="productViewModel">Modelo de dados com as informações atualizadas.</param>
    /// <returns>A mesma View de edição com o resultado da operação.</returns>
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
}