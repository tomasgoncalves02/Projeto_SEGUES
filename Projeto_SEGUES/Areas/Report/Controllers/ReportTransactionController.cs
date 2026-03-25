using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Areas.Report.ViewModels;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Report.Controllers;

/// <summary>
/// Controller responsible for generating user financial movement reports.
/// </summary>
/// <remarks>
/// This controller manages the query and filtering of transaction history, allowing users
/// to audit their top-ups and debits performed within the SEGUES system.
/// </remarks>
[Authorize]
[Area("Report")]
public class ReportTransactionController : Controller
{
    private readonly IReportService _reportService;

    /// <summary>
    /// Initializes a new instance of the transaction report controller.
    /// </summary>
    public ReportTransactionController(IReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>
    /// Displays the main page for financial movement history.
    /// </summary>
    /// <returns>
    /// The Index View populated with the logged-in user's transaction list.
    /// Redirects to a global error page if the database query fails.
    /// </returns>
    public async Task<IActionResult> Index(ReportTransactionSearchViewModel searchModel)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Challenge();

        searchModel.Results = await _reportService.GetTransactionHistoryAsync(userId, searchModel);
        return View(searchModel);
    }

    /// <summary>
    /// Filters the transaction history based on search criteria, type, and date.
    /// </summary>
    /// <param name="searchString">Search term for transaction description or reference.</param>
    /// <param name="typeFilter">Filter for "In" (positive) or "Out" (negative) movements.</param>
    /// <param name="dateFilter">Minimum date for inclusion in the report.</param>
    /// <returns>A PartialView containing the filtered table rows, or 500 status on error.</returns>
    [HttpGet]
    public async Task<IActionResult> GetFilteredBalance(ReportTransactionSearchViewModel searchModel)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) 
        {
            Response.Headers["HX-Redirect"] = Url.Page("/Account/Login", new { area = "Identity" });
            return Challenge();
        }
        
        var results = await _reportService.GetTransactionHistoryAsync(userId, searchModel);
        return PartialView("_BalanceHistoryRows", results);
    }
}