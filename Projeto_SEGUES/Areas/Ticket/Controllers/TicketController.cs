using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using System.Security.Claims;

namespace Projeto_SEGUES.Areas.Ticket;

[Authorize]
[Area("Ticket")]
public class TicketController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly ITicketService _ticketService;
    private readonly RoleManager<Role> _roleManager;
    private readonly AppDbContext _context;

    public TicketController(UserManager<AppUser> userManager, RoleManager<Role> roleManager, ITicketService ticketService, AppDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _ticketService = ticketService;
        _context = context;
    }

    // Canteen
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        ViewBag.UserBalance = user.Balance;
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
    public async Task<IActionResult> TransferTickets(List<string> SelectedTickets, string RecipientEmail)
    {
        var currentUserId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(currentUserId)) return Challenge();

        if (SelectedTickets == null || !SelectedTickets.Any())
        {
            TempData["Error"] = "Por favor, selecione pelo menos uma senha para transferir.";
            return RedirectToAction(nameof(SendTicket));
        }

        if (string.IsNullOrWhiteSpace(RecipientEmail))
        {
            TempData["Error"] = "O e-mail do destinatário é obrigatório.";
            return RedirectToAction(nameof(SendTicket));
        }

        // Chama o serviço
        var result = await _ticketService.TransferTicketsAsync(currentUserId, RecipientEmail, SelectedTickets);

        if (!result.Success)
        {
            TempData["Error"] = result.Message;
        }
        else
        {
            TempData["Success"] = result.Message;
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