using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Projeto_SEGUES.Areas.Admin.Controllers;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Services;

namespace SeguesTests.SecurityTests.Admin;

public class AdminMenuManagementSecurityTests
{
    private readonly Mock<IAdminService> _adminMock;
    private readonly AdminMenuManagementController _controller;

    public AdminMenuManagementSecurityTests()
    {
        _adminMock = new Mock<IAdminService>();
        var loggerMock = new Mock<ILogger<AdminMenuManagementController>>();
        _controller = new AdminMenuManagementController(_adminMock.Object, loggerMock.Object);
    }

    [Fact]
    public async Task SaveLinks_ModelStateInvalid_PreventsDatabaseUpdate()
    {
        _controller.ModelState.AddModelError("CanteenUrl", "URL Inválido");
        var model = new MenuManagementViewModel { CanteenUrl = "link-errado", BarUrl = "https://ips.pt" };

           
        var result = await _controller.SaveLinks(model);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("Index", viewResult.ViewName);

        _adminMock.Verify(s => s.UpdateMenuLinksAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SaveLinks_EmptyModel_ReturnsViewWithOriginalModel()
    {
        _controller.ModelState.AddModelError("Error", "Required");
        var emptyModel = new MenuManagementViewModel();

        var result = await _controller.SaveLinks(emptyModel);

           
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(emptyModel, viewResult.Model);
    }
}