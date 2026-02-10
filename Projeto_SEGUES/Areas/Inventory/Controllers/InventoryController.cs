using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Projeto_SEGUES.Areas.Inventory.Controllers;

[Authorize]
[Area("Inventory")]
public class InventoryController : Controller
{
    // GET
    public IActionResult Index()
    {
        return View();
    }
}