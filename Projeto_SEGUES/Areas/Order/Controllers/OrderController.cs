using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Areas.Order.ViewModels;
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

    /// <summary>
    /// Initializes a new instance of the controller with user management, administration, and logging services.
    /// </summary>
    public OrderController(
        UserManager<AppUser> userManager,
        IAdminService adminService,
        IOrderService orderService)
    {
        _userManager = userManager;
        _adminService = adminService;
        _orderService = orderService;
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
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();
        
        BarCanteenConfigViewModel config = await _adminService.GetScheduleAsync();
        var cart = await _orderService.GetCartAsync(user.Id, false);
        
        var now = DateTime.Now;
        var currentTime = now.TimeOfDay;
        var today = now.DayOfWeek;
        
        bool closedByWeekendConfig = (today == DayOfWeek.Saturday && !config.IsOpenSaturday) ||
                                     (today == DayOfWeek.Sunday && !config.IsOpenSunday);
        bool isOutsideHours = (currentTime < config.BarOpeningTime || currentTime > config.BarClosingTime);

        OrderPageViewModel vm = new OrderPageViewModel
        {
            UserBalance = user.Balance.ToString("C"),
            CartTotal = cart != null ? _orderService.GetOrderTotal(cart) : new OrderTotalViewModel(),
            BarOpeningTimeString = config.BarOpeningTimeString!,
            BarClosingTimeString = config.BarClosingTimeString!,
            BarMenuLink = config.BarMenuLink!,
            IsOpenSaturday = config.IsOpenSaturday,
            IsOpenSunday = config.IsOpenSunday,
            IsClosedByWeekend = closedByWeekendConfig,
            IsOutsideHours = isOutsideHours
        };
        
        return View(vm);
    }
}