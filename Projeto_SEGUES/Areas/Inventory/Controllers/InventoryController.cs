using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Projeto_SEGUES.Areas.Inventory.Controllers;

/// <summary>
/// Controller principal para a gestão e visualização do inventário de produtos.
/// </summary>
/// <remarks>
/// Este controlador pertence à área "Inventory" e exige que o utilizador esteja autenticado 
/// para aceder às funcionalidades de listagem e consulta de stock.
/// </remarks>
[Authorize]
[Area("Inventory")]
public class InventoryController : Controller
{
    /// <summary>
    /// Apresenta a página inicial do módulo de inventário.
    /// </summary>
    /// <returns>A View correspondente ao índice do inventário.</returns>
    /// <remarks>
    /// Geralmente utilizado para carregar a interface base onde os produtos serão listados.
    /// </remarks>
    public IActionResult Index()
    {
        return View();
    }
}