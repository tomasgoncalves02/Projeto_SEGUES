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
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            BarCanteenConfigViewModel barCanteenConfig = await _adminService.GetScheduleAsync();
            ViewBag.UserBalance = user.Balance;
            return View(barCanteenConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao carregar a página principal de senhas.");

            var msg = $"{Errors.DatabaseQueryError} [Erro: {(int)AppErrors.DatabaseQueryError}]";
            TempData.SetSwalError(msg);

            return RedirectToAction("Index", "Home", new { area = "" });
        }
    }

    /// <summary>
    /// Lists all active tickets for the current user.
    /// </summary>
    public async Task<IActionResult> ActiveTickets()
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var activeTickets = await _ticketService.GetActiveTicketsAsync(user.Id);
            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.UserRole = roles.FirstOrDefault();

            return View(activeTickets);
        }
        catch (Exception ex)
        {
            // CORRIGIDO: Ordem dos parâmetros de acordo com LoggerExtensions
            _logger.LogAppError(AppErrors.DatabaseQueryError, TableName.Ticket, AppOperation.Read, ex);

            var msg = $"{Errors.DatabaseQueryError} [Erro: {(int)AppErrors.DatabaseQueryError}]";
            TempData.SetSwalError(msg);

            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Displays the view to select and send tickets.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> SendTicket()
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var availableTickets = await _ticketService.GetActiveTicketsAsync(user.Id);
            return View(availableTickets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao carregar página de envio de senhas.");
            TempData.SetSwalError(Errors.UnexpectedError);
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Updates the ticket table via AJAX/HTMX.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUpdatedTickets()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var tickets = await _ticketService.GetUserTicketsAsync(userId);
            return PartialView("_TicketTablePartial", tickets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro AJAX no histórico de senhas.");
            return StatusCode(500, new { failMessage = Errors.DatabaseQueryError });
        }
    }

    /// <summary>
    /// Updates the active tickets grid via AJAX/HTMX.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUpdatedActiveTickets()
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var activeTickets = await _ticketService.GetActiveTicketsAsync(user.Id);
            return PartialView("_ActiveTicketsPartial", activeTickets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro AJAX nas senhas ativas.");
            return StatusCode(500, new { failMessage = Errors.DatabaseQueryError });
        }
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
            TempData.SetSwalError(Errors.NoItemsSelected);
            return RedirectToAction(nameof(SendTicket));
        }

        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            TempData.SetSwalError(Errors.RecipientEmailRequired);
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

            if (currentUser == null) return Unauthorized();

            // Validação: Destinatário não existe
            if (recipient == null)
                return Json(new { success = false, message = Errors.UserNotFound });

            if (currentUser.UserCategory.Id != recipient.UserCategory.Id)
            {
                var msg = string.Format(Errors.CategoryMismatch,
                                        recipient.UserCategory.Name,
                                        currentUser.UserCategory.Name);

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