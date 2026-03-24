using Microsoft.AspNetCore.Mvc;

namespace Projeto_SEGUES.Areas.Statistics.Controllers
{
    [Area("Statistics")]
    public class StatisticsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
