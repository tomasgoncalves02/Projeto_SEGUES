using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Resources;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Statistics.Controllers
{
    /// <summary>
    /// Controller responsible for generating and providing analytical data regarding orders.
    /// </summary>
    /// <remarks>
    /// This controller is restricted to Administrators and provides data for visual charts, 
    /// allowing for periodic analysis of order volume and business performance.
    /// </remarks>
    [Area("Statistics")]
    [Authorize(Roles = "Admin")]
    public class OrdersStatisticsController : Controller
    {
        private readonly IStatisticsService _statisticsService;
        private readonly ILogger<OrdersStatisticsController> _logger;

        /// <summary>
        /// Initializes a new instance of the statistics controller with specialized services and logging.
        /// </summary>
        public OrdersStatisticsController(IStatisticsService statisticsService, ILogger<OrdersStatisticsController> logger)
        {
            _statisticsService = statisticsService;
            _logger = logger;
        }

        /// <summary>
        /// Displays the main statistics dashboard view.
        /// </summary>
        /// <returns>The Index View for the statistics area.</returns>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Retrieves order statistics for a specific period to be consumed by frontend charts.
        /// </summary>
        /// <param name="period">The analysis period in days (default is 1).</param>
        /// <returns>A JSON result containing statistical data or a 500 status on service failure.</returns>
        [HttpGet]
        public async Task<IActionResult> GetOrdersStats(int period = 1)
        {
            try
            {
                // Busca os dados através do serviço de estatísticas
                var result = await _statisticsService.GetOrdersStats(period);

                return Json(result);
            }
            catch (Exception ex)
            {
                // Regista a falha no sistema de auditoria
                _logger.LogAppError(AppErrors.DatabaseQueryError,
                                    TableName.Order,
                                    AppOperation.Read, ex);

                // Retorna um erro amigável usando a tua classe de recursos
                var msg = $"{Errors.DatabaseQueryError} [Erro: {(int)AppErrors.DatabaseQueryError}]";

                return StatusCode(500, new { failMessage = msg });
            }
        }
    }
}