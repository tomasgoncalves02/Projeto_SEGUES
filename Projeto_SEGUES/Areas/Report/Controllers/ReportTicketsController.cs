using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Projeto_SEGUES.Areas.Report;

[Authorize]
[Area("Report")]
public class ReportTicketsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}