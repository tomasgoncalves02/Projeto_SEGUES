using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Audit.ViewModels;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using System.Diagnostics;

namespace Projeto_SEGUES.Controllers;

/// <summary>
/// Main controller for the application, handling public pages and the user dashboard landing.
/// </summary>
/// <remarks>
/// This controller manages the initial state of the application, including loading menu links,
/// displaying user balance, and handling global error responses.
/// </remarks>
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly UserManager<AppUser> _userManager;
    private readonly IAdminService _adminService;
    private readonly IOrderService _orderService;

    /// <summary>
    /// Initializes a new instance of the <see cref="HomeController"/>.
    /// </summary>
    public HomeController(
        ILogger<HomeController> logger,
        UserManager<AppUser> userManager,
        IAdminService adminService,
        IOrderService orderService)
    {
        _logger = logger;
        _adminService = adminService;
        _userManager = userManager;
        _orderService = orderService;
    }

    /// <summary>
    /// Renders the main index page. 
    /// </summary>
    /// <remarks>
    /// For unauthenticated users, it shows the public landing. 
    /// For authenticated users, it hydrates the view with personal data such as balance, 
    /// role, and current cart totals.
    /// </remarks>
    /// <returns>The Index View with relevant dashboard data in the ViewBag.</returns>
    public async Task<IActionResult> Index()
    {
        // Load external menu configuration (Canteen and Bar links)
        var barCanteenConfigViewModel = await _adminService.GetMenuLinksAsync();
        ViewBag.CanteenLink = barCanteenConfigViewModel.CanteenMenuLink;
        ViewBag.BarLink = barCanteenConfigViewModel.BarMenuLink;

        // Check if the user is logged in
        if (User.Identity is not { IsAuthenticated: true }) return View();

        // If logged, load profile data for the dashboard view
        var user = await _userManager.GetUserAsync(User);

        if (user != null)
        {
            ViewBag.UserBalance = user.Balance;
            ViewBag.FirstName = user.FirstName;
            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.UserRole = roles.FirstOrDefault();

            // Retrieve current cart summary if exists
            var cart = await _orderService.GetCartAsync(user.Id, false);
            if (cart != null)
            {
                ViewBag.CartTotal = _orderService.GetOrderTotal(cart);
            }
        }
        else
        {
            // Log security or data integrity error if Identity context exists but user record is missing
            _logger.LogAppError(AppErrors.UserNotFound, TableName.Identity, AppOperation.Read);
            return RedirectToAction("Error", "Home", new { area = "", errorCode = AppErrors.UserNotFound });
        }

        return View();
    }

    /// <summary>
    /// Displays the privacy policy page.
    /// </summary>
    public IActionResult Privacy()
    {
        return View();
    }

    /// <summary>
    /// Displays the operating schedule for the Bar and Canteen facilities.
    /// </summary>
    /// <returns>A View populated with the current schedule configuration.</returns>
    public async Task<IActionResult> Schedule()
    {
        BarCanteenConfigViewModel barCanteenConfig = await _adminService.GetScheduleAsync();
        return View(barCanteenConfig);
    }

    /// <summary>
    /// Global error handling action.
    /// </summary>
    /// <param name="errorCode">Optional application-specific error code.</param>
    /// <returns>The Error View with diagnostic information and localized messages.</returns>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(AppErrors? errorCode = null)
    {
        AppErrors code = errorCode ?? AppErrors.InternalServerError;

        // Retrieve the localized error message based on the enum extension
        var errorMessage = code.GetViewErrorMessage();

        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            ErrorCode = (int)code,
            ErrorMessage = errorMessage
        });
    }
}