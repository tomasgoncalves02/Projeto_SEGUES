using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Audit.ViewModels;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Resources;
using Projeto_SEGUES.Services;
using System.Diagnostics;
using Xunit.Sdk;

namespace Projeto_SEGUES.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        //private readonly IStringLocalizer<Errors> _localizer;
        private readonly UserManager<AppUser> _userManager;
        private readonly IAdminService _adminService;
        private readonly IOrderService _orderService;
        private readonly IStringLocalizer _localizer;
        public HomeController(
            ILogger<HomeController> logger, 
            //IStringLocalizer<Errors> localizer, 
            UserManager<AppUser> userManager, 
            IAdminService adminService,
            IOrderService orderService,
            IStringLocalizerFactory factory)
        {
            _logger = logger;
            //_localizer = localizer;
            _localizer = factory.Create(typeof(Projeto_SEGUES.Resources.Errors));
            _adminService = adminService;
            _userManager = userManager;
            _orderService = orderService;
        }

        public async Task<IActionResult> Index()
        {
            var barCanteenConfigViewModel = await _adminService.GetMenuLinksAsync();
            ViewBag.CanteenLink = barCanteenConfigViewModel.CanteenMenuLink;
            ViewBag.BarLink = barCanteenConfigViewModel.BarMenuLink;
            
            // Check if logged
            if (User.Identity is not { IsAuthenticated: true }) return View();

            // If logged, load data for view
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
                _logger.LogAppError(
                    Errors.ResourceManager.GetString(nameof(AppErrors.UserNotFound), System.Globalization.CultureInfo.InvariantCulture),
                    TableName.Identity, AppOperation.Read);
                return RedirectToAction("Error", "Home", new { errorCode = AppErrors.UserNotFound });
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public async Task<IActionResult> Schedule()
        {
            BarCanteenConfigViewModel barCanteenConfig = await _adminService.GetScheduleAsync();
            return View(barCanteenConfig);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(AppErrors? errorCode = null, params object[] args)
        {         
            AppErrors code = errorCode ?? AppErrors.InternalServerError;
         
            var errorMessage = _localizer[code.ToString(), args].Value;

            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                ErrorCode = code,
                ErrorMessage = errorMessage 
            });
        }
    }
}