using Microsoft.AspNetCore.Mvc;
using Moq;
using Projeto_SEGUES.Areas.Report;
using Projeto_SEGUES.Areas.Report.Controllers;
using Projeto_SEGUES.Areas.Report.ViewModels;
using Projeto_SEGUES.Models.Ticket; 
using Projeto_SEGUES.Services;
using Xunit;

namespace SeguesTests.UnitTests.Report
{
    public class ReportStatisticsTicketUnitTests
    {
        private readonly Mock<IReportService> _mockService;
        private readonly ReportStatisticsTicketController _controller;

        public ReportStatisticsTicketUnitTests()
        {
            _mockService = new Mock<IReportService>();
            _controller = new ReportStatisticsTicketController(_mockService.Object);
        }

        [Fact]
        public void Index_ReturnsViewResult()
        {
            var result = _controller.Index();
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task GetTicketsStats_ReturnsJsonWithData()
        {
            var mockData = new ReportStatisticsTicketDto
            {
                TotalUsedTickets = 50,
                TotalRevenue = 125.00m,
                Chart = new List<ChartDataDto>(),
                ByCategory = new List<CategoryDataDto>()
            };

            _mockService.Setup(s => s.GetTicketsStats(It.IsAny<int>()))
                .ReturnsAsync(mockData);

            var result = await _controller.GetTicketsStats(1);

            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.Equal(mockData, jsonResult.Value);
        }
    }
}