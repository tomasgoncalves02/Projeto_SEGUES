using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using Projeto_SEGUES.Areas.Admin.Controllers;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Services;

namespace SeguesTests.RegressionTests.Admin;

public class AdminMenuManagementRegressionTests
{
    private readonly Mock<IAdminService> _adminMock;
    private readonly AdminMenuManagementController _controller;

    public AdminMenuManagementRegressionTests()
    {
        _adminMock = new Mock<IAdminService>();
        var loggerMock = new Mock<ILogger<AdminMenuManagementController>>();
        var tempDataMock = new Mock<ITempDataDictionary>();
        _controller = new AdminMenuManagementController(_adminMock.Object, loggerMock.Object)
        {
            TempData = tempDataMock.Object
        };
    }

    [Fact]
    public async Task Index_HandlesNullLinksFromService_ReturnsEmptyModel()
    {
        var configFromService = new BarCanteenConfigViewModel
        {
            CanteenMenuLink = null,
            BarMenuLink = null
        };

        _adminMock.Setup(s => s.GetMenuLinksAsync()).ReturnsAsync(configFromService);

        var result = await _controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<MenuManagementViewModel>(viewResult.Model);
        Assert.Null(model.CanteenUrl);
        Assert.Null(model.BarUrl);
    }

    [Fact]
    public async Task SaveLinks_DatabaseException_PreservesPedroInputInView()
    {
        _adminMock.Setup(s => s.UpdateMenuLinksAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Erro Crítico de DB"));

        var model = new MenuManagementViewModel
        {
            CanteenUrl = "https://ementa-pedro.pt",
            BarUrl = "https://bar-pedro.pt"
        };

        var result = await _controller.SaveLinks(model);

        var viewResult = Assert.IsType<ViewResult>(result);
        var modelReturned = Assert.IsType<MenuManagementViewModel>(viewResult.Model);

        Assert.Equal("https://ementa-pedro.pt", modelReturned.CanteenUrl);
        Assert.Equal("https://bar-pedro.pt", modelReturned.BarUrl);
        Assert.Equal("Index", viewResult.ViewName);
    }

    [Fact]
    public async Task SaveLinks_SuccessfulUpdate_LogsUserAction()
    {
        var model = new MenuManagementViewModel
        {
            CanteenUrl = "https://ips.pt",
            BarUrl = "https://ips.pt"
        };

        await _controller.SaveLinks(model);

        _adminMock.Verify(s => s.UpdateMenuLinksAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }
}