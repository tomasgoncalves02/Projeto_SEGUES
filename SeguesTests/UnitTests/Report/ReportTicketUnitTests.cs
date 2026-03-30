using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Projeto_SEGUES.Areas.Report.ViewModels;
using Projeto_SEGUES.Services;
using System.Security.Claims;
using Projeto_SEGUES.Areas.Report.Controllers;

namespace SeguesTests.UnitTests.Report;

public class ReportTicketUnitTests
{
    private readonly Mock<ITicketService> _mockService;
    private readonly ReportTicketController _controller;

    public ReportTicketUnitTests()
    {
        _mockService = new Mock<ITicketService>();
        _controller = new ReportTicketController(_mockService.Object);
    }

    private void SetupUser(string userId)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    [Fact]
    public async Task Index_ReturnsViewWithModel()
    {
        SetupUser("pedro-77");
        var model = new ReportTicketSearchViewModel();
        _mockService.Setup(s => s.GetTicketHistoryAsync("pedro-77", model))
            .ReturnsAsync([]);

        var result = await _controller.Index(model);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(model, viewResult.Model);
        Assert.Equal("pedro-77", _controller.ViewBag.UserId);
    }

    [Fact]
    public async Task GetFilteredHistory_ReturnsPartialView()
    {
        SetupUser("pedro-77");
        var model = new ReportTicketSearchViewModel();
        _mockService.Setup(s => s.GetTicketHistoryAsync("pedro-77", model))
            .ReturnsAsync([]);

        var result = await _controller.GetFilteredHistory(model);

        var partialResult = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_TicketHistoryRowsPartial", partialResult.ViewName);
    }
}