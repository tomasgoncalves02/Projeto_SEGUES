using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Projeto_SEGUES.Models.Audit.ViewModels;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using System.Diagnostics;

namespace Projeto_SEGUES.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IStringLocalizer<AppErrors> _localizer;
        private readonly UserManager<AppUser> _userManager;
        private readonly IAdminService _adminService;
        private readonly IOrderService _orderService;
        public HomeController(
            ILogger<HomeController> logger, 
            IStringLocalizer<AppErrors> localizer, 
            UserManager<AppUser> userManager, 
            IAdminService adminService,
            IOrderService orderService)
        {
            _logger = logger;
            _localizer = localizer;
            _adminService = adminService;
            _userManager = userManager;
            _orderService = orderService;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.CanteenLink = await _adminService.GetCanteenMenuLinkAsync();
            ViewBag.BarLink = await _adminService.GetBarMenuLinkAsync();
            
            // Check if logged
            if (User.Identity is not { IsAuthenticated: true }) return View();

            // If logged load data for view
            var user = await _userManager.GetUserAsync(User);

            if (user != null)
            {
                ViewBag.UserBalance = user.Balance;
                ViewBag.FirstName = user.FirstName;
                var roles = await _userManager.GetRolesAsync(user);
                ViewBag.UserRole = roles.FirstOrDefault();
                var cart = await _orderService.GetCartAsync(user.Id, false);
                if (cart != null)
                {
                    ViewBag.CartTotal = _orderService.GetOrderTotal(cart);
                }
            }
            else
            {
                _logger.LogError(null, _localizer[nameof(AppErrors.UserNotFound)], "Error", TableName.Identity, AppOperation.Read);
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public async Task<IActionResult> Schedule()
        {
            var open = await _adminService.GetOpenBarTimeAsync();
            var close = await _adminService.GetCloseBarTimesAsync();

            ViewBag.OpeningTime = open.ToString(@"hh\:mm");
            ViewBag.ClosingTime = close.ToString(@"hh\:mm");

            ViewBag.LunchOpenTime = await _adminService.GetOpenLunchTimeAsync();
            ViewBag.LunchCloseTime = await _adminService.GetCloseLunchTimeAsync();
            ViewBag.DinnerOpenTime = await _adminService.GetOpenDinnerTimeAsync();
            ViewBag.DinnerCloseTime = await _adminService.GetCloseDinnerTimeAsync();

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}