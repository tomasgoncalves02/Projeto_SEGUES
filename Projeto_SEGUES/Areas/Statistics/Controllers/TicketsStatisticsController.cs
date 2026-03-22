using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Resources;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Statistics.Controllers
{
    /// <summary>
    /// Controller responsible for providing analytical data regarding ticket usage and sales.
    /// </summary>
    /// <remarks>
    /// Access is restricted to Administrators. This controller serves as an API for 
    /// dashboard charts, processing ticket distribution and status over time.
    /// </remarks>
    [Area("Statistics")]
    [Authorize(Roles = "Admin")]
    public class TicketsStatisticsController : Controller
    {
        private readonly IStatisticsService _statisticsService;
        private readonly ILogger<TicketsStatisticsController> _logger;
        private readonly IStringLocalizer<Errors> _localizer;

        /// <summary>
        /// Initializes a new instance of the tickets statistics controller.
        /// </summary>
        /// <param name="statisticsService">Service for ticket data aggregation.</param>
        /// <param name="logger">Logger for auditing and error tracking.</param>
        /// <param name="localizer">Localizer for retrieving localized error messages from resources.</param>
        public TicketsStatisticsController(
            IStatisticsService statisticsService,
            ILogger<TicketsStatisticsController> logger,
            IStringLocalizer<Errors> localizer)
        {
            _statisticsService = statisticsService;
            _logger = logger;
            _localizer = localizer;
        }

        /// <summary>
        /// Displays the ticket statistics dashboard.
        /// </summary>
        /// <returns>The Index View for ticket analytics.</returns>
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Retrieves aggregated ticket statistics for a specific period in JSON format.
        /// </summary>
        /// <param name="period">The analysis period in days.</param>
        /// <returns>A JSON result with statistics or a localized error message on failure.</returns>
        [HttpGet]
        public async Task<IActionResult> GetTicketsStats(int period = 1)
        {
            try
            {
                var result = await _statisticsService.GetTicketsStats(period);
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogAppError(AppErrors.DatabaseQueryError,
                                    TableName.Ticket,
                                    AppOperation.Read, ex);
                var errorKey = AppErrors.DatabaseQueryError.ToString();
                var msg = $"{_localizer[errorKey].Value} [Erro: {(int)AppErrors.DatabaseQueryError}]";

                return StatusCode(500, new { failMessage = msg });
            }
        }
    }
}