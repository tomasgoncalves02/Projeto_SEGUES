using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Extensions;
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
        private readonly UserManager<AppUser> _userManager;
        private readonly IAdminService _adminService;
        public HomeController(ILogger<HomeController> logger, UserManager<AppUser> userManager, IAdminService adminService)
        {
            _logger = logger;
            _adminService = adminService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // Check if logged
            if (User.Identity is { IsAuthenticated: true })
            {
                //If logged load dashboard data
                var user = await _userManager.GetUserAsync(User);

                if (user != null)
                {
                    ViewBag.UserBalance = user.Balance;
                    ViewBag.FirstName = user.FirstName;
                    var roles = await _userManager.GetRolesAsync(user);
                    ViewBag.UserRole = roles.FirstOrDefault();
                }
                else
                {
                    _logger.LogInformation("User not found.");
                    return View(); 
                }
            }

            // Skips to this if isn´t logged
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