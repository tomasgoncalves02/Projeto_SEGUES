using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Report;

/// <summary>
/// Controller responsible for providing analytical data regarding ticket usage and sales.
/// </summary>
/// <remarks>
/// Access is restricted to Administrators. This controller serves as an API for 
/// dashboard charts, processing ticket distribution and status over time.
/// </remarks>
[Area("Report")]
[Authorize(Roles = "Admin")]
public class ReportStatisticsTicketController : Controller
{
    private readonly IReportService _reportService;

    /// <summary>
    /// Initializes a new instance of the tickets statistics controller.
    /// </summary>
    /// <param name="reportService">Service for ticket data aggregation.</param>
    /// <param name="logger">Logger for auditing and error tracking.</param>
    /// <param name="localizer">Localizer for retrieving localized error messages from resources.</param>
    public ReportStatisticsTicketController(IReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>
    /// Displays the ticket statistics dashboard.
    /// </summary>
    /// <returns>The Index View for ticket analytics.</returns>
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    /// Retrieves aggregated ticket statistics for a specific period in JSON format.
    /// </summary>
    /// <param name="period">The analysis period in days.</param>
    /// <returns>A JSON result with statistics or a localized error message on failure.</returns>
    [HttpGet]
    public async Task<IActionResult> GetTicketsStats(int period = 1)
    {
        var result = await _reportService.GetTicketsStats(period);
        return Json(result);
    }
}