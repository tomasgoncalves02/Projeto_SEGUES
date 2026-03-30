using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Areas.Ticket.ViewModels;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Ticket.Controllers;

/// <summary>
/// Controller responsible for the physical or digital validation of user tickets.
/// </summary>
/// <remarks>
/// Access is restricted to Admin and Employee roles. This controller provides the interface 
/// for checking ticket codes and updating their status to "Used" in real-time.
/// </remarks>
[Authorize(Roles = "Admin,Employee")]
[Area("Ticket")]
public class TicketValidationController : Controller
{
    private readonly ITicketService _ticketService;
    private readonly UserManager<AppUser> _userManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="TicketValidationController"/>.
    /// </summary>
    public TicketValidationController(UserManager<AppUser> userManager, ITicketService ticketService)
    {
        _userManager = userManager;
        _ticketService = ticketService;
    }

    /// <summary>
    /// Displays the ticket validation dashboard.
    /// </summary>
    /// <returns>A View containing the validation form and a list of recently processed tickets.</returns>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var model = new ValidateTicketViewModel
        {
            RecentTickets = await _ticketService.GetRecentUsedTicketsAsync()
        };
        return View(model);
    }

    /// <summary>
    /// Processes the validation of a submitted ticket code.
    /// </summary>
    /// <param name="model">The ViewModel containing the ticket code entered by the operator.</param>
    /// <returns>
    /// The updated Index view with a success or error message (SweetAlert) and the refreshed 
    /// list of recent tickets.
    /// </returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ValidateTicketViewModel model)
    {
        if (!ModelState.IsValid)
        {
            // Refresh recent tickets if model validation fails
            model.RecentTickets = await _ticketService.GetRecentUsedTicketsAsync();
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        // Performs the business logic for ticket verification via the service layer
        var result = _ticketService.ValidateTicketAsync(model.Code!, user);

        if (result.Result.Success)
            TempData.SetSwalSuccess(result.Result.Message);
        else
            TempData.SetSwalError(result.Result.Message);

        // Clear the form to prevent double submission and prepare for the next code
        ModelState.Clear();
        model.Code = string.Empty;

        // Refresh recent tickets for operator feedback
        model.RecentTickets = await _ticketService.GetRecentUsedTicketsAsync();
        return View(model);
    }
}