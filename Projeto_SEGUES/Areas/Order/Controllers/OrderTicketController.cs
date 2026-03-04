using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Order;

[Authorize]
[Area("Order")]
public class OrderTicketController : Controller
{
    private readonly ITicketService _ticketService;
    private readonly UserManager<AppUser> _userManager;
    
    public OrderTicketController(ITicketService ticketService, UserManager<AppUser> userManager)
    {
        _ticketService = ticketService;
        _userManager = userManager;
    }
    
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();
        
        //Get current price for this user category
        decimal currentPrice = await _ticketService.GetCurrentPriceForUserAsync(user);

        ViewBag.UserBalance = user.Balance;
        ViewBag.CurrentPrice = currentPrice;

        // Get tickets for this user
        var myTickets = await _ticketService.GetUserTicketsAsync(user.Id);
        return View(myTickets);
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BuyTicket(int quantity = 1)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Challenge();
        
        var result = await _ticketService.BuyTicketsAsync(userId, quantity);

        if (result.Success)
            TempData.SetSwalSuccess(result.Message);
        else
            TempData.SetSwalError(result.Message);

        return RedirectToAction(nameof(Index));
    }
}