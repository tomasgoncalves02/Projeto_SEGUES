using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Statistics.Controllers
{
    [Area("Statistics")]
    [Authorize(Roles = "Admin")]
    public class OrdersStatisticsController : Controller
    {
        private readonly IStatisticsService _statisticsService;


        public OrdersStatisticsController(IStatisticsService statisticsService)
        {
            _statisticsService = statisticsService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetOrdersStats(int period = 1)
        {
            var result = await _statisticsService.GetOrdersStats(period);
            return Json(result);
        }

    }
}
