using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Projeto_SEGUES.Controllers
{
    
    [Authorize(Roles = "ExternalEmployee")]
    public class ExternalEmployeeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}