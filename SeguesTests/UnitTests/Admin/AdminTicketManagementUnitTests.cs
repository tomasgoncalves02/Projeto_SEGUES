using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Projeto_SEGUES.Areas.Admin.Controllers;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Areas.Report.ViewModels;
using Projeto_SEGUES.Services;

namespace SeguesTests.UnitTests.Admin;

public class AdminTicketManagementUnitTests
{
    private readonly Mock<IAdminService> _mockAdminService;
    private readonly Mock<ITicketService> _mockTicketService;
    private readonly AdminTicketManagementController _controller;

    public AdminTicketManagementUnitTests()
    {
        _mockAdminService = new Mock<IAdminService>();
        _mockTicketService = new Mock<ITicketService>();
        var mockPdfService = new Mock<IPdfService>();
        _controller = new AdminTicketManagementController(_mockAdminService.Object, _mockTicketService.Object, mockPdfService.Object);
        _controller.TempData = new Mock<ITempDataDictionary>().Object;
    }

    [Fact]
    public async Task Index_ReturnsView_WithCorrectViewModel()
    {
        var config = new BarCanteenConfigViewModel { CanteenLunchOpeningTimeString = "12:00", CanteenLunchClosingTimeString = "14:00" };
        _mockAdminService.Setup(s => s.GetScheduleAsync()).ReturnsAsync(config);
        _mockAdminService.Setup(s => s.GetTicketPricesAsync())
            .ReturnsAsync([]);
        _mockTicketService.Setup(s => s.GetTicketHistoryAsync(null, It.IsAny<ReportTicketSearchViewModel>())).ReturnsAsync(
            []);

        var result = await _controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<AdminTicketManagementViewModel>(viewResult.Model);
        Assert.Equal("12:00", model.LunchOpeningTime);
    }

    [Fact]
    public async Task UpdatePrices_EmptyList_RedirectsToIndex()
    {
        var result = await _controller.UpdatePrices([]);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        _mockAdminService.Verify(s => s.UpdateTicketPricesAsync(It.IsAny<List<TicketPriceUpdateDto>>()), Times.Never);
    }

    [Fact]
    public async Task UpdateValidity_PositiveDays_RedirectsWithSuccess()
    {
        _mockAdminService.Setup(s => s.UpdateTicketValidityDaysAsync(30)).ReturnsAsync(ServiceResult.Ok("Ok"));

        var result = await _controller.UpdateValidity(30);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }
}