using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

    public TicketController(UserManager<AppUser> userManager, ITicketService ticketService)
    {
        _userManager = userManager;
        _ticketService = ticketService;
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

    public async Task<IActionResult> SendTicket()
    {
        return View();
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
}