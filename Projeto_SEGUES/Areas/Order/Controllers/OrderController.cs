using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Order;

/// <summary>
/// Controller responsible for the home page of the orders module.
/// </summary>
/// <remarks>
/// This controller serves as the entry point for the user, providing essential information 
/// such as available balance, bar operating hours, and access to the digital menu.
/// </remarks>
[Authorize]
[Area("Order")]
public class OrderController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IAdminService _adminService;
    private readonly IOrderService _orderService;
    private readonly ILogger<OrderController> _logger;

    /// <summary>
    /// Initializes a new instance of the controller with user management, administration, and logging services.
    /// </summary>
    public OrderController(
        UserManager<AppUser> userManager,
        IAdminService adminService,
        IOrderService orderService,
        ILogger<OrderController> logger)
    {
        _userManager = userManager;
        _adminService = adminService;
        _orderService = orderService;
        _logger = logger;
    }

    /// <summary>
    /// Prepares and displays the home page of the orders area.
    /// </summary>
    /// <returns>
    /// The main orders View populated with the user's balance and operating hours. 
    /// Redirects to a global error page if data retrieval fails.
    /// </returns>
    /// <remarks>
    /// Opening and closing hours are formatted as strings for direct display in the UI.
    /// </remarks>
    public async Task<IActionResult> Index()
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);

            // Verificação real em vez de usar o operador '!'
            if (user == null) return Challenge();

            ViewBag.UserBalance = user.Balance;

            var cart = await _orderService.GetCartAsync(user.Id, false);
            if (cart != null)
            {
                ViewBag.CartTotal = _orderService.GetOrderTotal(cart);
            }

            BarCanteenConfigViewModel barCanteenConfig = await _adminService.GetScheduleAsync();
            ViewBag.BarOpeningTimeString = barCanteenConfig.BarOpeningTimeString;
            ViewBag.BarClosingTimeString = barCanteenConfig.BarClosingTimeString;
            ViewBag.BarMenuLink = barCanteenConfig.BarMenuLink;

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro fatal ao carregar o dashboard de encomendas.");
            return RedirectToAction("Error", "Home", new
            {
                area = "",
                errorCode = (int)AppErrors.DatabaseQueryError
            });
        }
    }
}