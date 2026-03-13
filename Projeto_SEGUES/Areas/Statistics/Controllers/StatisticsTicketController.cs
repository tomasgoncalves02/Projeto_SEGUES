using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Statistics.Controllers
{
    [Area("Statistics")]
    [Authorize(Roles = "Admin")]
    public class StatisticsTicketController : Controller
    {
        private readonly IStatisticsService _statisticsService;

        public StatisticsTicketController(IStatisticsService statisticsService)
        {
            _statisticsService = statisticsService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetTotalTickets()
        {
            var result = await _statisticsService.GetTotalTicketsAsync();
            return Json(result);
        }


    }
}