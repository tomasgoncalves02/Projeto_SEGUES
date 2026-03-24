using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using System.Security.Claims;
using Projeto_SEGUES.Areas.Ticket.ViewModels;

namespace Projeto_SEGUES.Areas.Ticket;

/// <summary>
/// Controller responsible for ticket management, including purchases, transfers, and status visualization.
/// </summary>
/// <remarks>
/// This controller integrates with AdminService for configurations and TicketService for business logic 
/// regarding the lifecycle of meal tickets.
/// </remarks>
[Authorize]
[Area("Ticket")]
public class TicketController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly ITicketService _ticketService;
    private readonly IAdminService _adminService;

    /// <summary>
    /// Initializes a new instance of the TicketController with necessary services and logging.
    /// </summary>
    public TicketController(
        UserManager<AppUser> userManager,
        ITicketService ticketService,
        IAdminService adminService)
    {
        _userManager = userManager;
        _ticketService = ticketService;
        _adminService = adminService;
    }

    /// <summary>
    /// Displays the main Canteen/Bar index page with current balance and schedules.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        BarCanteenConfigViewModel barCanteenConfig = await _adminService.GetScheduleAsync();
        ViewBag.UserBalance = user.Balance;
        return View(barCanteenConfig);
    }

    /// <summary>
    /// Lists all active tickets for the current user.
    /// </summary>
    public async Task<IActionResult> ActiveTickets()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var activeTickets = await _ticketService.GetActiveTicketsAsync(userId);
        return View(activeTickets);
    }

    /// <summary>
    /// Updates the ticket table via AJAX/HTMX.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUpdatedTickets()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            // Force the browser to do a full-page redirect
            Response.Headers["HX-Redirect"] = Url.Page("/Account/Login", new { area = "Identity" });
            return Unauthorized();
        }

        var tickets = await _ticketService.GetUserTicketsAsync(userId);
        return PartialView("_TicketTablePartial", tickets);
    }

    /// <summary>
    /// Updates the active tickets grid via AJAX/HTMX.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUpdatedActiveTickets()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            // Force the browser to do a full-page redirect
            Response.Headers["HX-Redirect"] = Url.Page("/Account/Login", new { area = "Identity" });
            return Unauthorized();
        }

        var activeTickets = await _ticketService.GetActiveTicketsAsync(userId);
        return PartialView("_ActiveTicketsPartial", activeTickets);
    }
    
    /// <summary>
    /// Displays the view to select and send tickets.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> TransferTicket()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var availableTickets = await _ticketService.GetActiveTicketsAsync(userId);
        TransferTicketViewModel vm = new TransferTicketViewModel
        {
            AvailableTickets = availableTickets
        };
        return View(vm);
    }
    
    /// <summary>
    /// Validates if the recipient is eligible for a transfer.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> CheckTransferEligibility(string email)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            // Force the browser to do a full-page redirect
            Response.Headers["HX-Redirect"] = Url.Page("/Account/Login", new { area = "Identity" });
            return Unauthorized();
        }
        
        var result = await _ticketService.CheckTransferEligibilityAsync(userId, email);
        return Json(new { success = result.Success, message = result.Message, recipientName = result.Data });
    }

    /// <summary>
    /// Processes the transfer of selected tickets to a recipient.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TransferTickets(TransferTicketViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        if (!ModelState.IsValid || model.SelectedTickets.Count == 0)
        {
            TempData.SetSwalError("Por favor, selecione pelo menos um item e verifique o email.");
            // Reload data
            model.AvailableTickets = await _ticketService.GetActiveTicketsAsync(userId);
            return View(nameof(TransferTicket), model);
        }
        
        var result = await _ticketService.TransferTicketsAsync(userId, model.RecipientEmail!, model.SelectedTickets);

        if (!result.Success)
        {
            TempData.SetSwalError(result.Message);
            
            // Reload data
            model.AvailableTickets = await _ticketService.GetActiveTicketsAsync(userId);
            return View(nameof(TransferTicket), model);
        }

        TempData.SetSwalSuccess(result.Message);
        return RedirectToAction(nameof(TransferTicket));
    }
}