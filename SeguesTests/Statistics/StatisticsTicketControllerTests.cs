using Microsoft.AspNetCore.Mvc;
using Moq;
using Projeto_SEGUES.Areas.Statistics.Controllers;
using Projeto_SEGUES.Services;
using Xunit;

namespace SeguesTests.Statistics
{
    public class StatisticsTicketControllerTests
    {
        private readonly Mock<IStatisticsService> _mockStatisticsService;
        private readonly StatisticsTicketController _controller;

        public StatisticsTicketControllerTests()
        {
            _mockStatisticsService = new Mock<IStatisticsService>();
            _controller = new StatisticsTicketController(_mockStatisticsService.Object);
        }


        // Confirms that the main ticket statistics dashboard is correctly loaded for the administrator
        [Fact]
        public void Index_ReturnsView()
        {
            var result = _controller.Index();
            Assert.IsType<ViewResult>(result);
        }


        // Ensures that meal ticket data is accurately retrieved from the service and returned in JSON format
        [Fact]
        public async Task GetTicketsStats_ValidPeriod_ReturnsJsonResult()
        {
            var testData = new { TotalTickets = 50, SalesAmount = 125.00m };
            _mockStatisticsService.Setup(s => s.GetTicketsStats(1)).ReturnsAsync(testData);

            var result = await _controller.GetTicketsStats(1);

            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.Equal(testData, jsonResult.Value);
        }


        // Validates the system's robustness by correctly handling periods with no recorded ticket sales
        [Fact]
        public async Task GetTicketsStats_NoDataFound_ReturnsJsonWithNull()
        {
            _mockStatisticsService.Setup(s => s.GetTicketsStats(It.IsAny<int>())).ReturnsAsync((object)null!);

            var result = await _controller.GetTicketsStats(1);

            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.Null(jsonResult.Value);
        }
    }
}