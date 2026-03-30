using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Projeto_SEGUES.Areas.Report.Controllers;

/// <summary>
/// Controller responsible for generating and viewing system reports.
/// </summary>
/// <remarks>
/// This controller belongs to the "Report" area and allows authenticated users 
/// to access statistical data, consumption history, or document exports.
/// </remarks>
[Authorize]
[Area("Report")]
public class ReportController : Controller
{
    /// <summary>
    /// Displays the main page of the reports module.
    /// </summary>
    /// <returns>The View corresponding to the reports and statistics index.</returns>
    /// <remarks>
    /// Serves as the central dashboard where the user can choose the type of report to generate.
    /// </remarks>
    public IActionResult Index()
    {
        return View();
    }
}