using Microsoft.AspNetCore.Mvc;

namespace Projeto_SEGUES.Controllers
{
    public class EmployeeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
