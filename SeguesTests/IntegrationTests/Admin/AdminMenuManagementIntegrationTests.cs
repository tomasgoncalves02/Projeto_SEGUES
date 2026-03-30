using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using Projeto_SEGUES.Areas.Admin.Controllers;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Services;

namespace SeguesTests.IntegrationTests.Admin;

public class AdminMenuManagementIntegrationTests
{
    private readonly Mock<IAdminService> _adminServiceMock;
    private readonly Mock<ITempDataDictionary> _tempDataMock;
    private readonly AdminMenuManagementController _controller;

    public AdminMenuManagementIntegrationTests()
    {
        _adminServiceMock = new Mock<IAdminService>();
        var loggerMock = new Mock<ILogger<AdminMenuManagementController>>();
        _tempDataMock = new Mock<ITempDataDictionary>();

        _controller = new AdminMenuManagementController(
            _adminServiceMock.Object,
            loggerMock.Object)
        {
            TempData = _tempDataMock.Object
        };
    }

    [Fact]
    public async Task Index_Integration_DisplaysCurrentDatabaseLinks()
    {
        var dbConfig = new BarCanteenConfigViewModel
        {
            CanteenMenuLink = "https://ips.pt/canteen-pedro",
            BarMenuLink = "https://ips.pt/bar-pedro"
        };

        _adminServiceMock.Setup(s => s.GetMenuLinksAsync()).ReturnsAsync(dbConfig);

        var result = await _controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<MenuManagementViewModel>(viewResult.Model);
        Assert.Equal("https://ips.pt/canteen-pedro", model.CanteenUrl);
        Assert.Equal("https://ips.pt/bar-pedro", model.BarUrl);
    }

    [Fact]
    public async Task SaveLinks_Integration_ExecutesUpdateAndTriggersSuccessAlert()
    {
        var model = new MenuManagementViewModel
        {
            CanteenUrl = "https://new-canteen.pt",
            BarUrl = "https://new-bar.pt"
        };

        var result = await _controller.SaveLinks(model);

        _adminServiceMock.Verify(s => s.UpdateMenuLinksAsync(model.CanteenUrl, model.BarUrl), Times.Once);

        _tempDataMock.VerifySet(t => t[It.Is<string>(k => k.Contains("Swal"))] = It.IsAny<object>(), Times.Once);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }

    [Fact]
    public async Task SaveLinks_Integration_DatabaseError_TriggersErrorAlertAndReturnsView()
    {
        _adminServiceMock.Setup(s => s.UpdateMenuLinksAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Database Connection Failed"));

        var model = new MenuManagementViewModel { CanteenUrl = "https://test.pt" };

        var result = await _controller.SaveLinks(model);

        _tempDataMock.VerifySet(t => t[It.Is<string>(k => k.Contains("Swal"))] = It.IsAny<object>(), Times.Once);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("Index", viewResult.ViewName);
    }
}