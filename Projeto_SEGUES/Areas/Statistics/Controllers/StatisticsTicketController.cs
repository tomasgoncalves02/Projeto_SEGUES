using Microsoft.AspNetCore.Mvc;

namespace Projeto_SEGUES.Areas.Statistics.Controllers
{
    [Area("Statistics")]
    public class StatisticsTicketController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
