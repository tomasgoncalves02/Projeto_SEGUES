using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Areas.Report.Controllers;

namespace SeguesTests.UnitTests.Report;

public class ReportStatisticsUnitTests
{
    private readonly ReportStatisticsController _controller;

    public ReportStatisticsUnitTests()
    {
        _controller = new ReportStatisticsController();
    }

    [Fact]
    public void Index_ReturnsViewResult()
    {
        var result = _controller.Index();

        Assert.IsType<ViewResult>(result);
    }
}