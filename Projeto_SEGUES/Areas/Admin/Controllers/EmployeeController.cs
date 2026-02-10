using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Projeto_SEGUES.Areas.Admin;

[Authorize(Roles = "Admin,Employee")]
[Area("Admin")]
public class EmployeeController : Controller
{
    // GET
    public IActionResult Index()
    {
        return View();
    }
}