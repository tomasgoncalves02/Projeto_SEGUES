using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Models;

namespace Projeto_SEGUES.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<User> _userManager; // Adicionado para gerir dados do utilizador

        public HomeController(ILogger<HomeController> logger, UserManager<User> userManager)
        {
            _logger = logger;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // 1. Verificação de Autenticação: Se não estiver logado, vai para o Login
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            // 2. Carregar o Utilizador da Base de Dados
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            // 3. EXCEÇÃO: O Funcionário continua a ser redirecionado para a sua área técnica
            if (User.IsInRole("Employee"))
            {
                return RedirectToAction("Index", "Employee");
            }

            // 4. PARA TODOS OS OUTROS: Não redirecionamos. 
            // Preparamos os dados e devolvemos a View do Dashboard (Home/Index)
            ViewBag.UserBalance = user.Balance;
            ViewBag.FirstName = user.FirstName;
            ViewBag.UserRole = user.Role;

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