using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Audit.ViewModels;
using Projeto_SEGUES.Models.User;

namespace Projeto_SEGUES.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<AppUser> _userManager;

        public HomeController(ILogger<HomeController> logger, UserManager<AppUser> userManager)
        {
            _logger = logger;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // If not logged, redirect to login
            if (User.Identity is not { IsAuthenticated: true })
            {
                // If already in login page, do not show the message
                if (Uri.TryCreate(Request.Headers.Referer, UriKind.Absolute, out var refererUri) &&
                 string.Equals(refererUri.AbsolutePath, "/Identity/Account/Login", StringComparison.OrdinalIgnoreCase))
                {
                 return RedirectToPage("/Account/Login", new { area = "Identity" });
                }
                if (Request.Headers.Referer.Equals("/Identity/Account/Login"))
                {
                    return RedirectToPage("/Account/Login", new { area = "Identity" });
                }
                _logger.LogInformation("User not authenticated.");
                TempData.SetSwalWarning("Faça login novamente. Desconectado por inatividade ou alteração dos dados.");
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            // Load user from database
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogInformation("User not found.");
                TempData.SetSwalError("Erro ao carregar dados do utilizador.");
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }
            
            // Prepare data and return the dashboard view
            ViewBag.UserBalance = user.Balance;
            ViewBag.FirstName = user.FirstName;
            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.UserRole = roles.FirstOrDefault();
            
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}