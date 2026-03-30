using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Controllers;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using System.Security.Claims;

namespace SeguesTests.SecurityTests.Home;

public class HomeControllerSecurityTests
{
    private readonly Mock<UserManager<AppUser>> _mockUserManager;
    private readonly Mock<IAdminService> _mockAdminService;
    private readonly HomeController _controller;

    public HomeControllerSecurityTests()
    {
        var mockLogger = new Mock<ILogger<HomeController>>();
        _mockAdminService = new Mock<IAdminService>();
        var mockOrderService = new Mock<IOrderService>();

        var store = new Mock<IUserStore<AppUser>>();
        _mockUserManager = new Mock<UserManager<AppUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _controller = new HomeController(mockLogger.Object, _mockUserManager.Object, _mockAdminService.Object, mockOrderService.Object);

        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    [Fact]
    public async Task Index_NotAuthenticated_ReturnsViewWithMenuLinksOnly()
    {
        var mockMenuLinks = new BarCanteenConfigViewModel { CanteenMenuLink = "public-canteen", BarMenuLink = "public-bar" };
        _mockAdminService.Setup(s => s.GetMenuLinksAsync()).ReturnsAsync(mockMenuLinks);
        _controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await _controller.Index();

        Assert.IsType<ViewResult>(result);
        Assert.Equal("public-canteen", _controller.ViewBag.CanteenLink);
        Assert.Equal("public-bar", _controller.ViewBag.BarLink);
        Assert.Null(_controller.ViewBag.FirstName);
    }

    [Fact]
    public async Task Index_UserNotFoundInDatabase_ReturnsRedirectToError()
    {
        var claims = new List<Claim> { new(ClaimTypes.Name, "pedro.ghost@segues.pt") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _controller.HttpContext.User = new ClaimsPrincipal(identity);

        var mockMenuLinks = new BarCanteenConfigViewModel();
        _mockAdminService.Setup(s => s.GetMenuLinksAsync()).ReturnsAsync(mockMenuLinks);
        _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((AppUser?)null);

        var result = await _controller.Index();

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
        Assert.True(redirectResult.RouteValues?.ContainsKey("errorCode"));
    }
}