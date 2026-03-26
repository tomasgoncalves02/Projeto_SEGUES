using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Order;

/// <summary>
/// Controller responsible for acquiring and viewing meal tickets (senhas).
/// </summary>
/// <remarks>
/// This controller manages the user's personal ticket inventory and the purchase process,
/// interacting with the ticket service to validate prices and user balances.
/// </remarks>
[Authorize]
[Area("Order")]
public class OrderTicketController : Controller
{
    private readonly ITicketService _ticketService;
    private readonly UserManager<AppUser> _userManager;

    /// <summary>
    /// Initializes a new instance of the controller with ticket, user, logging, and localization services.
    /// </summary>
    public OrderTicketController(
        ITicketService ticketService,
        UserManager<AppUser> userManager)
    {
        _ticketService = ticketService;
        _userManager = userManager;
    }

    /// <summary>
    /// Displays the ticket management page for the authenticated user.
    /// </summary>
    /// <returns>
    /// A View with the user's tickets and price/balance info. 
    /// Redirects to the error page if data retrieval fails.
    /// </returns>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        // Get the current price of a ticket for this user
        decimal currentPrice = await _ticketService.GetCurrentPriceForUserAsync(user);

        ViewBag.UserBalance = user.Balance;
        ViewBag.CurrentPrice = currentPrice;

        // Get user tickets
        var myTickets = await _ticketService.GetUserTicketsAsync(user.Id);
        return View(myTickets);
    }

    /// <summary>
    /// Processes the purchase of one or more meal tickets.
    /// </summary>
    /// <param name="quantity">Number of tickets to purchase (default is 1).</param>
    /// <returns>Redirects to Index with a success or error message via SweetAlert.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BuyTicket(int quantity = 1)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Challenge();
        
        var result = await _ticketService.BuyTicketsAsync(userId, quantity);

        if (result.Success)
        {
            TempData.SetSwalSuccess(result.Message);
            return RedirectToAction("Index", "ActiveOrder", new { area = "Order" });
        }
        TempData.SetSwalError(result.Message);
        return RedirectToAction(nameof(Index));
    }
}