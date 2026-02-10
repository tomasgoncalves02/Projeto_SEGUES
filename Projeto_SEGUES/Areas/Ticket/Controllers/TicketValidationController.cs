using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Areas.Ticket.ViewModels;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Ticket;

[Authorize(Roles = "Admin,Employee")]
[Area("Ticket")]
public class TicketValidationController : Controller
{
    private readonly ITicketService _ticketService;
    private readonly UserManager<AppUser> _userManager;
    
    public TicketValidationController(UserManager<AppUser> userManager, ITicketService ticketService)
    {
        _userManager = userManager;
        _ticketService = ticketService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var model = new ValidateTicketViewModel
        {
            RecentTickets = await _ticketService.GetRecentUsedTicketsAsync()
        };
        return View(model);
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ValidateTicketViewModel model)
    {
        if (!ModelState.IsValid)
        {
            // Refresh recent tickets
            model.RecentTickets = await _ticketService.GetRecentUsedTicketsAsync();
            return View(model);
        }
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var result = _ticketService.ValidateTicketAsync(model.Code!, user);
        if (result.Result.Success)
            TempData.SetSwalSuccess(result.Result.Message);
        else
            TempData.SetSwalError(result.Result.Message);
        
        // Clear the form
        ModelState.Clear();
        model.Code = string.Empty;

        // Refresh recent tickets for display
        model.RecentTickets = await _ticketService.GetRecentUsedTicketsAsync();
        return View(model);
    }
}