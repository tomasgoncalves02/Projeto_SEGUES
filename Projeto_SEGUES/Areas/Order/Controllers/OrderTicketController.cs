using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Resources;
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
    private readonly ILogger<OrderTicketController> _logger;
    private readonly IStringLocalizer<Errors> _localizer;

    /// <summary>
    /// Initializes a new instance of the controller with ticket, user, logging, and localization services.
    /// </summary>
    public OrderTicketController(
        ITicketService ticketService,
        UserManager<AppUser> userManager,
        ILogger<OrderTicketController> logger,
        IStringLocalizer<Errors> localizer)
    {
        _ticketService = ticketService;
        _userManager = userManager;
        _logger = logger;
        _localizer = localizer;
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
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Obter o preço atual para a categoria deste utilizador
            decimal currentPrice = await _ticketService.GetCurrentPriceForUserAsync(user);

            ViewBag.UserBalance = user.Balance;
            ViewBag.CurrentPrice = currentPrice;

            // Obter a lista de senhas deste utilizador
            var myTickets = await _ticketService.GetUserTicketsAsync(user.Id);
            return View(myTickets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro fatal ao carregar o inventário de senhas.");

            // 1001 - DatabaseQueryError
            return RedirectToAction("Error", "Home", new
            {
                area = "",
                errorCode = (int)AppErrors.DatabaseQueryError
            });
        }
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
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        try
        {
            var result = await _ticketService.BuyTicketsAsync(userId, quantity);

            if (result.Success)
            {
                TempData.SetSwalSuccess(result.Message);
            }
            else
            {
                TempData.SetSwalError(result.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogAppError($"Erro crítico na compra de senhas para o utilizador {userId}: {ex.Message}",
                                TableName.Ticket,
                                AppOperation.Create);

            // 1004 - DatabaseUpdateError
            var erroEnum = AppErrors.DatabaseUpdateError;
            var msg = $"{_localizer[erroEnum.ToString()].Value} [Erro: {(int)erroEnum}]";

            TempData.SetSwalError(msg);
        }
        return RedirectToAction(nameof(Index));
    }
}