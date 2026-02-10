using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Projeto_SEGUES.Areas.Purchase;

[Authorize]
[Area("Purchase")]
public class PurchaseBarController : Controller
{
    // GET
    public IActionResult Index()
    {
        return View();
    }
}