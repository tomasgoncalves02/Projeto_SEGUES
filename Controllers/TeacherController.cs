using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Projeto_SEGUES.Controllers
{
    [Authorize(Roles = "Teacher")] // <--- Só Docentes entram aqui
    public class TeacherController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}