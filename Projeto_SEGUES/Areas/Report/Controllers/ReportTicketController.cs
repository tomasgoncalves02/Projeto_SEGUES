using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Areas.Report.ViewModels;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Report;

/// <summary>
/// Controller responsible for viewing and filtering the user's ticket history.
/// </summary>
/// <remarks>
/// This controller allows users to audit their ticket usage, filtering by state 
/// (Available, Used, Expired), flow (Purchased, Received, Sent), and specific dates.
/// </remarks>
[Authorize]
[Area("Report")]
public class ReportTicketController : Controller
{
    private readonly ITicketService _ticketService;

    /// <summary>
    /// Initializes a new instance of the controller with ticket, user, and logging services.
    /// </summary>
    public ReportTicketController(
        ITicketService ticketService)
    {
        _ticketService = ticketService;
    }

    /// <summary>
    /// Displays the main ticket history page with support for multiple filters.
    /// </summary>
    /// <param name="searchString">Search term for codes or related users.</param>
    /// <param name="stateFilter">Filter by ticket state (TicketState Enum).</param>
    /// <param name="flowFilter">Flow filter (e.g., "Sent", "Received").</param>
    /// <param name="dateFilter">Filter by transaction or usage date.</param>
    /// <returns>The Index View populated with query results. Redirects to error on failure.</returns>
    [HttpGet]
    public async Task<IActionResult> Index(ReportTicketSearchViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Challenge();

        model.Results = await _ticketService.QueryHistoryAsync(userId, model);
        
        ViewBag.UserId = userId;
        return View(model);
    }

    /// <summary>
    /// Endpoint for dynamic (AJAX/HTMX) updates of the ticket history table.
    /// </summary>
    /// <param name="stateFilter">Ticket state in string format for conversion.</param>
    /// <param name="dateFilter">Selected date for filtering.</param>
    /// <param name="flowFilter">Transaction flow type.</param>
    /// <param name="searchString">Alphanumeric search term.</param>
    /// <returns>A PartialView containing only the filtered table rows, or 500 on error.</returns>
    [HttpGet]
    public async Task<IActionResult> GetFilteredHistory(ReportTicketSearchViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) 
        {
            Response.Headers["HX-Redirect"] = Url.Page("/Account/Login", new { area = "Identity" });
            return Unauthorized();
        }
        
        ViewBag.UserId = userId;

        var history = await _ticketService.QueryHistoryAsync(userId, model);
        return PartialView("_TicketHistoryRowsPartial", history);
    }
}