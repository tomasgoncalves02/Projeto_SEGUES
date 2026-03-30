using Microsoft.AspNetCore.Mvc;
using Moq;
using Projeto_SEGUES.Areas.Report.Controllers;
using Projeto_SEGUES.Areas.Report.ViewModels;
using Projeto_SEGUES.Services;

namespace SeguesTests.UnitTests.Report;

public class ReportStatisticsOrderUnitTests
{
    private readonly Mock<IReportService> _mockService;
    private readonly ReportStatisticsOrderController _controller;

    public ReportStatisticsOrderUnitTests()
    {
        _mockService = new Mock<IReportService>();
        _controller = new ReportStatisticsOrderController(_mockService.Object);
    }

    [Fact]
    public void Index_ReturnsViewResult()
    {
        var result = _controller.Index();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task GetOrdersStats_ReturnsJsonWithData()
    {
        var mockStats = new ReportStatisticsOrderDto { TotalOrders = 10 };
        _mockService.Setup(s => s.GetOrdersStats(It.IsAny<int>()))
            .ReturnsAsync(mockStats);

        var result = await _controller.GetOrdersStats();

        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.Equal(mockStats, jsonResult.Value);
    }
}