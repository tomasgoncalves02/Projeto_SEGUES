using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Moq;
using Projeto_SEGUES.Areas.Report;
using Projeto_SEGUES.Resources;
using Projeto_SEGUES.Services;

namespace SeguesTests.Statistics
{
    public class ReportStatisticsTicketControllerTests
    {
        private readonly Mock<IReportService> _mockStatisticsService;
        private readonly ReportStatisticsTicketController _ticketController;
        private readonly Mock<ILogger<ReportStatisticsTicketController>> _mockLogger;
        private readonly Mock<IStringLocalizer<Errors>> _mockLocalizer;

        public ReportStatisticsTicketControllerTests()
        {
            _mockStatisticsService = new Mock<IReportService>();
            _mockLogger = new Mock<ILogger<ReportStatisticsTicketController>>();
            _mockLocalizer = new Mock<IStringLocalizer<Errors>>();
            _ticketController = new ReportStatisticsTicketController(_mockStatisticsService.Object);
        }


        // Confirms that the main ticket statistics dashboard is correctly loaded for the administrator
        [Fact]
        public void Index_ReturnsView()
        {
            var result = _ticketController.Index();
            Assert.IsType<ViewResult>(result);
        }


        // Ensures that meal ticket data is accurately retrieved from the service and returned in JSON format
        [Fact]
        public async Task GetTicketsStats_ValidPeriod_ReturnsJsonResult()
        {
            var testData = new { TotalTickets = 50, SalesAmount = 125.00m };
            //_mockStatisticsService.Setup(s => s.GetTicketsStats(1)).ReturnsAsync(testData);

            var result = await _ticketController.GetTicketsStats(1);

            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.Equal(testData, jsonResult.Value);
        }


        // Validates the system's robustness by correctly handling periods with no recorded ticket sales
        [Fact]
        public async Task GetTicketsStats_NoDataFound_ReturnsJsonWithNull()
        {
            //_mockStatisticsService.Setup(s => s.GetTicketsStats(It.IsAny<int>())).ReturnsAsync((object)null!);

            var result = await _ticketController.GetTicketsStats(1);

            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.Null(jsonResult.Value);
        }
    }
}