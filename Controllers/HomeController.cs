using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Models;

namespace Projeto_SEGUES.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            // 1. Se não estiver logado, manda para o Login
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            // 2. Se estiver logado, verifica a Role e redireciona
            if (User.IsInRole("Admin"))
            {
                return RedirectToAction("Index", "Admin");
            }

            if (User.IsInRole("Teacher"))
            {
                return RedirectToAction("Index", "Teacher");
            }

            if (User.IsInRole("Student"))
            {
                return RedirectToAction("Index", "Student");
            }

            if (User.IsInRole("Employee") )
            {
                return RedirectToAction("Index", "Employee");
            }

            if ( User.IsInRole("ExternalEmployee"))
            {
                return RedirectToAction("Index", "ExternalEmployee");
            }

            // 3. Fallback (Se tiver logado mas não tiver role nenhuma conhecida)
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
