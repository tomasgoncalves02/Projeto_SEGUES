using Microsoft.AspNetCore.Mvc;
using Moq;
using Projeto_SEGUES.Areas.Statistics.Controllers;
using Projeto_SEGUES.Services;

namespace SeguesTests.Statistics
{
    public class OrdersStatisticsControllerTests
    {
        private readonly Mock<IStatisticsService> _mockStatisticsService;
        private readonly OrdersStatisticsController _controller;

        public OrdersStatisticsControllerTests()
        {
            _mockStatisticsService = new Mock<IStatisticsService>();
            _controller = new OrdersStatisticsController(_mockStatisticsService.Object);
        }

        // Confirms that the index action successfully returns the statistics dashboard view
        [Fact]
        public void Index_ReturnsView()
        {
            var result = _controller.Index();
            Assert.IsType<ViewResult>(result);
        }

        // Ensures bar statistics are retrieved and returned as a JSON result for a specific period
        [Fact]
        public async Task GetBarStats_ValidPeriod_ReturnsJsonResult()
        {
            var testData = new { TotalSales = 100, Revenue = 500.00m };
            _mockStatisticsService.Setup(s => s.GetOrdersStats(1)).ReturnsAsync(testData);

            var result = await _controller.GetOrdersStats(1);

            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.Equal(testData, jsonResult.Value);
        }

        // Verifies that the service is called with the correct period parameter
        [Fact]
        public async Task GetBarStats_CallsService_WithCorrectParameter()
        {
            int period = 7;
            await _controller.GetOrdersStats(period);

            _mockStatisticsService.Verify(s => s.GetOrdersStats(period), Times.Once);
        }

        // Ensures the controller handles cases where no statistical data is found for the given period
        [Fact]
        public async Task GetBarStats_NoDataFound_ReturnsEmptyJson()
        {
            _mockStatisticsService.Setup(s => s.GetOrdersStats(It.IsAny<int>())).ReturnsAsync((object)null!);

            var result = await _controller.GetOrdersStats(1);

            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.Null(jsonResult.Value);
        }
    }
}