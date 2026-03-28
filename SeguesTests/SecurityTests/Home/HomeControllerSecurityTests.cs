using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Moq;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Controllers;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Resources;
using Projeto_SEGUES.Services;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace SeguesTests.SecurityTests.Home
{
    public class HomeControllerSecurityTests
    {
        private readonly Mock<ILogger<HomeController>> _mockLogger;
        private readonly Mock<IStringLocalizer<Errors>> _mockLocalizer;
        private readonly Mock<UserManager<AppUser>> _mockUserManager;
        private readonly Mock<IAdminService> _mockAdminService;
        private readonly Mock<IOrderService> _mockOrderService;
        private readonly HomeController _controller;

        public HomeControllerSecurityTests()
        {
            _mockLogger = new Mock<ILogger<HomeController>>();
            _mockLocalizer = new Mock<IStringLocalizer<Errors>>();
            _mockAdminService = new Mock<IAdminService>();
            _mockOrderService = new Mock<IOrderService>();

            var store = new Mock<IUserStore<AppUser>>();
            _mockUserManager = new Mock<UserManager<AppUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            _controller = new HomeController(_mockLogger.Object, _mockLocalizer.Object, _mockUserManager.Object, _mockAdminService.Object, _mockOrderService.Object);

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

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("public-canteen", _controller.ViewBag.CanteenLink);
            Assert.Equal("public-bar", _controller.ViewBag.BarLink);
            Assert.Null(_controller.ViewBag.FirstName);
        }

        [Fact]
        public async Task Index_UserNotFoundInDatabase_ReturnsRedirectToError()
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "pedro.ghost@segues.pt") };
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
}