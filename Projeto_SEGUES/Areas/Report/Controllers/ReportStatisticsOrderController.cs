using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Report;

/// <summary>
/// Controller responsible for generating and providing analytical data regarding orders.
/// </summary>
/// <remarks>
/// This controller is restricted to Administrators and provides data for visual charts, 
/// allowing for periodic analysis of order volume and business performance.
/// </remarks>
[Area("Report")]
[Authorize(Roles = "Admin")]
public class ReportStatisticsOrderController : Controller
{
    private readonly IReportService _reportService;

    /// <summary>
    /// Initializes a new instance of the statistics controller with specialized services and logging.
    /// </summary>
    public ReportStatisticsOrderController(IReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>
    /// Displays the main statistics dashboard view.
    /// </summary>
    /// <returns>The Index View for the statistics area.</returns>
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    /// Retrieves order statistics for a specific period to be consumed by frontend charts.
    /// </summary>
    /// <param name="period">The analysis period in days (default is 1).</param>
    /// <returns>A JSON result containing statistical data or a 500 status on service failure.</returns>
    [HttpGet]
    public async Task<IActionResult> GetOrdersStats(int period = 1)
    {
        var result = await _reportService.GetOrdersStats(period);
        return Json(result);
    }
}