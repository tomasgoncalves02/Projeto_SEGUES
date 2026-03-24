using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Projeto_SEGUES.Areas.Report;

[Area("Report")]
[Authorize(Roles = "Admin")]
public class ReportStatisticsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}