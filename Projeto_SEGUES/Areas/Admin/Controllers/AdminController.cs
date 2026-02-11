using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace Projeto_SEGUES.Areas.Admin;

[Authorize(Roles = "Admin")]
[Area("Admin")]
public class AdminController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
