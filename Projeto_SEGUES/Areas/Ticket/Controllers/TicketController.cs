using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using System.Security.Claims;
using Projeto_SEGUES.Extensions;

namespace Projeto_SEGUES.Areas.Ticket;

[Authorize]
[Area("Ticket")]
public class TicketController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly ITicketService _ticketService;
    private readonly AppDbContext _context;
    private readonly IAdminService _adminService;

    public TicketController(UserManager<AppUser> userManager, ITicketService ticketService, AppDbContext context, IAdminService adminService)
    {
        _userManager = userManager;
        _ticketService = ticketService;
        _context = context;
        _adminService = adminService;
    }

    // Canteen
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        ViewBag.UserBalance = user.Balance;
        ViewBag.LunchOpenTime = await _adminService.GetOpenLunchTimeAsync();
        ViewBag.LunchCloseTime = await _adminService.GetCloseLunchTimeAsync();
        ViewBag.DinnerOpenTime = await _adminService.GetOpenDinnerTimeAsync();
        ViewBag.DinnerCloseTime = await _adminService.GetCloseDinnerTimeAsync();
        ViewBag.RefeitorioLink = await _adminService.GetRefeitorioMenuLinkAsync();
        return View();
    }

    public async Task<IActionResult> ActiveTickets()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        // Filter only available tickets (not yet used or expired)
        var activeTickets = await _ticketService.GetActiveTicketsAsync(user.Id);

        // Get role
        var roles = await _userManager.GetRolesAsync(user);
        ViewBag.UserRole = roles.FirstOrDefault();

        return View(activeTickets);
    }

    [HttpGet]
    public async Task<IActionResult> SendTicket()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var availableTickets = await _ticketService.GetActiveTicketsAsync(user.Id);

        return View(availableTickets);
    }

    [HttpGet]
    public async Task<IActionResult> GetUpdatedTickets()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        // Agora sem ambiguidade
        var tickets = await _ticketService.GetUserTicketsAsync(userId);

        // Certifica-te que a vista está em Views/Shared/ para ser encontrada aqui
        return PartialView("_TicketTable", tickets);
    }

    [HttpGet]
    public async Task<IActionResult> GetUpdatedActiveTickets()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        // Utiliza o serviço para buscar apenas as senhas ativas (Available)
        var activeTickets = await _ticketService.GetActiveTicketsAsync(user.Id);

        // Retorna apenas a Partial View para o htmx atualizar os cartões
        return PartialView("_ActiveTicketsCards", activeTickets);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TransferTickets(List<string> selectedTickets, string recipientEmail)
    {
        var currentUserId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(currentUserId)) return Challenge();

        if (selectedTickets == null || !selectedTickets.Any())
        {
            TempData.SetSwalError("Por favor, selecione pelo menos uma senha para transferir.");
            return RedirectToAction(nameof(SendTicket));
        }

        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            TempData.SetSwalError("O e-mail do destinatário é obrigatório.");
            return RedirectToAction(nameof(SendTicket));
        }

        // Chama o serviço
        var result = await _ticketService.TransferTicketsAsync(currentUserId, recipientEmail, selectedTickets);

        if (!result.Success)
        {
            TempData.SetSwalError(result.Message);
        }
        else
        {
            TempData.SetSwalSuccess(result.Message);
        }

        return RedirectToAction(nameof(SendTicket));
    }

    public async Task<IActionResult> CheckTransferEligibility(string email)
    {
        var currentUserId = _userManager.GetUserId(User);

        var currentUser = await _context.Users
            .Include(u => u.UserCategory)
            .FirstOrDefaultAsync(u => u.Id == currentUserId);

        // 3. Carregar o destinatário também com a categoria
        var recipient = await _context.Users
            .Include(u => u.UserCategory)
            .FirstOrDefaultAsync(u => u.Email == email);

        if (currentUser == null) return Unauthorized();

        if (recipient == null)
        {
            return Json(new { success = false, message = "Utilizador não encontrado." });
        }

        if (currentUser.UserCategory.Id != recipient.UserCategory.Id)
        {
            return Json(new
            {
                success = false,
                message = $"Não é permitido transferir senhas para utilizadores da categoria '{recipient.UserCategory.Name}'. " +
                          $"Apenas pode enviar para outros '{currentUser.UserCategory.Name}'."
            });
        }

        return Json(new
        {
            success = true,
            recipientName = $"{recipient.FirstName} {recipient.LastName}"
        });
    }
}