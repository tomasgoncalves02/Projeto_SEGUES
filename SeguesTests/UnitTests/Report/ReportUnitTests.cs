using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Areas.Report.Controllers;

namespace SeguesTests.UnitTests.Report;

public class ReportUnitTests
{
    private readonly ReportController _controller;

    public ReportUnitTests()
    {
        _controller = new ReportController();
    }

    [Fact]
    public void Index_ReturnsViewResult()
    {
        var result = _controller.Index();

        Assert.IsType<ViewResult>(result);
    }
}