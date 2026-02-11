using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Projeto_SEGUES.Areas.Report;

[Authorize]
[Area("Report")]
public class ReportController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}