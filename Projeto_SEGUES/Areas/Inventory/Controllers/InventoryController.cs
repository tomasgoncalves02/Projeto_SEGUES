using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Projeto_SEGUES.Areas.Inventory.Controllers;

/// <summary>
/// Main controller for managing and viewing the product inventory.
/// </summary>
/// <remarks>
/// This controller belongs to the "Inventory" area and requires the user to be authenticated 
/// to access stock listing and consultation features.
/// </remarks>
[Authorize]
[Area("Inventory")]
public class InventoryController : Controller
{
    /// <summary>
    /// Displays the home page of the inventory module.
    /// </summary>
    /// <returns>The View corresponding to the inventory index.</returns>
    /// <remarks>
    /// Typically used to load the base interface where products will be listed.
    /// </remarks>
    public IActionResult Index()
    {
        return View();
    }
}