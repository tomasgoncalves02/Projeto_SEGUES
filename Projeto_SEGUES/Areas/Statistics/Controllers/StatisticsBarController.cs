using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Statistics.Controllers
{
    [Area("Statistics")]
    public class StatisticsBarController : Controller
    {
        private readonly IStatisticsService _statisticsService;


        public StatisticsBarController(IStatisticsService statisticsService)
        {
            _statisticsService = statisticsService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetBarStats(string period = "Dia")
        {
            var result = await _statisticsService.GetTicketsStats(period);
            return Json(result);
        }

    }
}
