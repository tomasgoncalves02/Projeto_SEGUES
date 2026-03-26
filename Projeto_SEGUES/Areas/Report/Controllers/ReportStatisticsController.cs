using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Projeto_SEGUES.Areas.Report;

/// <summary>
/// Controller responsible for high-level administrative statistical analysis.
/// </summary>
/// <remarks>
/// Unlike the general Report controller, this module is restricted to users with the "Admin" role, 
/// as it provides access to sensitive consolidated data and system-wide performance metrics.
/// </remarks>
[Area("Report")]
[Authorize(Roles = "Admin")]
public class ReportStatisticsController : Controller
{
    /// <summary>
    /// Displays the main dashboard for administrative statistics.
    /// </summary>
    /// <returns>The View corresponding to the advanced statistics index.</returns>
    /// <remarks>
    /// Serves as the primary interface for visualizing charts, trends, and 
    /// global system indicators.
    /// </remarks>
    public IActionResult Index()
    {
        return View();
    }
}