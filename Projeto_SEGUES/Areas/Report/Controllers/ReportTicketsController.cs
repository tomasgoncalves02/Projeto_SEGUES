using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Report;

[Authorize]
[Area("Report")]
public class ReportTicketsController : Controller
{
    private readonly ITicketService _ticketService;
    private readonly UserManager<AppUser> _userManager;
    
    public ReportTicketsController(ITicketService ticketService, UserManager<AppUser> userManager)
    {
        _ticketService = ticketService;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string searchString, TicketState? stateFilter, string flowFilter, DateTime? dateFilter)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Challenge();

        // Passamos a data selecionada para o serviço
        var tickets = await _ticketService.QueryHistoryAsync(userId, searchString, stateFilter, flowFilter, dateFilter);

        ViewData["CurrentSearch"] = searchString;
        ViewData["CurrentState"] = stateFilter;
        ViewData["CurrentFlow"] = flowFilter;
        ViewData["CurrentDate"] = dateFilter; 
        ViewBag.CurrentUserId = userId;

        return View(tickets);
    }
}