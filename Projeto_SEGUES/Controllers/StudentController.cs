using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Models;

namespace Projeto_SEGUES.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        // 1. Declaramos a variável
        private readonly UserManager<User> _userManager;

        // 2. Criamos o CONSTRUTOR (Obrigatório para o _userManager não ser null)
        public StudentController(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // 3. Agora o código já consegue ir buscar o utilizador
            var user = await _userManager.GetUserAsync(User);

            if (user == null) return Challenge();

            // 4. Passamos o utilizador para a View (Resolve o erro da imagem cc7b95)
            return View(user);
        }
    }
}