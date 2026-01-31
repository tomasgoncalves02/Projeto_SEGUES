using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Models; // Necessário para aceder ao modelo User
using Projeto_SEGUES.Data;   // Necessário para o AppDbContext

namespace Projeto_SEGUES.Controllers
{
    [Authorize(Roles = "External")]
    public class ExternalController : Controller
    {
        // 1. DECLARAÇÃO DOS SERVIÇOS
        private readonly UserManager<User> _userManager;
        private readonly AppDbContext _context;

     
        public ExternalController(UserManager<User> userManager, AppDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // 3. OBTER O UTILIZADOR
            // Agora o _userManager já não é nulo porque foi injetado no construtor
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            // 4. PASSAR O SALDO (Parte 3) PARA A VIEW (Parte 2)
            // Isto resolve o erro de "null reference" na tua página
            ViewBag.UserBalance = user.Balance;

            return View(user);
        }
    }
}