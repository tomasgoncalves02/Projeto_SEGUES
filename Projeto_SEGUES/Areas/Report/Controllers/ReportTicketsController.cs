using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Resources;
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
public class ReportTicketsController : Controller
{
    private readonly ITicketService _ticketService;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<ReportTicketsController> _logger;

    /// <summary>
    /// Initializes a new instance of the controller with ticket, user, and logging services.
    /// </summary>
    public ReportTicketsController(
        ITicketService ticketService,
        UserManager<AppUser> userManager,
        ILogger<ReportTicketsController> logger)
    {
        _ticketService = ticketService;
        _userManager = userManager;
        _logger = logger;
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
    public async Task<IActionResult> Index(string? searchString, TicketState? stateFilter, string? flowFilter, DateTime? dateFilter)
    {
        try
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();

            var tickets = await _ticketService.QueryHistoryAsync(userId, searchString, stateFilter, flowFilter, dateFilter);

            ViewData["CurrentSearch"] = searchString;
            ViewData["CurrentState"] = stateFilter;
            ViewData["CurrentFlow"] = flowFilter;
            ViewData["CurrentDate"] = dateFilter;
            ViewBag.CurrentUserId = userId;

            return View(tickets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro fatal ao carregar o histórico de senhas (Index).");

            // Usando a tua chave 'DatabaseQueryError' e o Enum 1001
            return RedirectToAction("Error", "Home", new
            {
                area = "",
                errorCode = (int)AppErrors.DatabaseQueryError
            });
        }
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
    public async Task<IActionResult> GetFilteredHistory(string? stateFilter, DateTime? dateFilter, string? flowFilter, string? searchString)
    {
        try
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            ViewBag.CurrentUserId = userId;

            TicketState? state = null;
            if (!string.IsNullOrEmpty(stateFilter) && Enum.TryParse<TicketState>(stateFilter, out var parsedState))
            {
                state = parsedState;
            }

            var history = await _ticketService.QueryHistoryAsync(userId, searchString, state, flowFilter, dateFilter);

            return PartialView("_TicketHistoryRows", history);
        }
        catch (Exception ex)
        {
            _logger.LogAppError($"Erro AJAX na filtragem de senhas: {ex.Message}", TableName.Ticket, AppOperation.Read);

            // Para chamadas parciais (AJAX), devolvemos a mensagem técnica do RESX via JSON/Header
            var msg = $"{Errors.DatabaseQueryError} [Erro: {(int)AppErrors.DatabaseQueryError}]";
            return StatusCode(500, new { failMessage = msg });
        }
    }
}