using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Resources;
using Projeto_SEGUES.Services;
using System.Security.Claims;

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
    private readonly AppDbContext _context;
    private readonly IAdminService _adminService;
    private readonly ILogger<TicketController> _logger;

    /// <summary>
    /// Initializes a new instance of the TicketController with necessary services and logging.
    /// </summary>
    public TicketController(
        UserManager<AppUser> userManager,
        ITicketService ticketService,
        AppDbContext context,
        IAdminService adminService,
        ILogger<TicketController> logger)
    {
        _userManager = userManager;
        _ticketService = ticketService;
        _context = context;
        _adminService = adminService;
        _logger = logger;
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
    /// Displays the view to select and send tickets.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> SendTicket()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var availableTickets = await _ticketService.GetActiveTicketsAsync(userId); 
        return View(availableTickets);
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
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var activeTickets = await _ticketService.GetActiveTicketsAsync(user.Id);
        return PartialView("_ActiveTicketsPartial", activeTickets);
    }

    /// <summary>
    /// Processes the transfer of selected tickets to a recipient.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TransferTickets(List<string> selectedTickets, string recipientEmail)
    {
        var currentUserId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(currentUserId)) return Challenge();

        if (selectedTickets == null || !selectedTickets.Any())
        {
            TempData.SetSwalError("Por favor, selecione pelo menos um item para continuar.");
            return RedirectToAction(nameof(SendTicket));
        }

        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            TempData.SetSwalError("O e-mail do destinatário é obrigatório");
            return RedirectToAction(nameof(SendTicket));
        }

        try
        {
            var result = await _ticketService.TransferTicketsAsync(currentUserId, recipientEmail, selectedTickets);

            if (!result.Success) TempData.SetSwalError(result.Message);
            else TempData.SetSwalSuccess(result.Message);
        }
        catch (Exception ex)
        {
            // CORRIGIDO: Removida a string personalizada para bater certo com LoggerExtensions
            _logger.LogAppError(AppErrors.DatabaseUpdateError, TableName.Ticket, AppOperation.Update, ex);

            var msg = string.Format(Errors.DatabaseUpdateError, "Ticket");
            TempData.SetSwalError(msg);
        }

        return RedirectToAction(nameof(SendTicket));
    }

    /// <summary>
    /// Validates if the recipient is eligible for a transfer.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> CheckTransferEligibility(string email)
    {
        try
        {
            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == null) return Unauthorized();

            // Precisamos do Include para carregar as categorias, senão dá erro de Null
            var currentUser = await _context.Users
                .Include(u => u.UserCategory)
                .FirstOrDefaultAsync(u => u.Id == currentUserId);

            var recipient = await _context.Users
                .Include(u => u.UserCategory)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (currentUser == null) return Challenge();

            // Validação: Destinatário não existe
            if (recipient == null)
                return Json(new { success = false, message = Errors.UserNotFound });

            if (currentUser.UserCategory.Id != recipient.UserCategory.Id)
            {
                var msg = "Transferência recusada: Só pode enviar senhas para utilizadores da mesma categoria.";

                return Json(new { success = false, message = msg });
            }
            return Json(new
            {
                success = true,
                recipientName = $"{recipient.FirstName} {recipient.LastName}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao verificar elegibilidade de transferência.");
            return Json(new { success = false, message = Errors.InternalServerError });
        }
    }
}