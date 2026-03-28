using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Moq;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Areas.Order.ViewModels;
using Projeto_SEGUES.Controllers;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Order;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Resources;
using Projeto_SEGUES.Services;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace SeguesTests.UnitTests.Home
{
    public class HomeControllerUnitTests
    {
        private readonly Mock<ILogger<HomeController>> _mockLogger;
        private readonly Mock<IStringLocalizer<Errors>> _mockLocalizer;
        private readonly Mock<UserManager<AppUser>> _mockUserManager;
        private readonly Mock<IAdminService> _mockAdminService;
        private readonly Mock<IOrderService> _mockOrderService;
        private readonly HomeController _controller;

        public HomeControllerUnitTests()
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
        public async Task Index_AuthenticatedUser_ReturnsViewWithDashboardData()
        {
            var user = new AppUser
            {
                Id = "pedro-77",
                FirstName = "Pedro",
                LastName = "Original",
                Balance = 15.5m,
                Email = "pedro@segues.pt",
                BirthDate = DateTime.Now.AddYears(-20),
                Gender = Gender.Male,
                UserCategory = new UserCategory { Name = "Estudante" }
            };

            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "pedro@segues.pt") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            _controller.HttpContext.User = new ClaimsPrincipal(identity);

            var mockMenuLinks = new BarCanteenConfigViewModel { CanteenMenuLink = "canteen-url", BarMenuLink = "bar-url" };
            var mockCart = new Order { Id = 1, TotalValue = 5.0m, AppUser = user };
            _mockAdminService.Setup(s => s.GetMenuLinksAsync()).ReturnsAsync(mockMenuLinks);
            _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
            _mockUserManager.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Cliente" });
            _mockOrderService.Setup(o => o.GetCartAsync(user.Id, false)).ReturnsAsync(mockCart);
            var mockOrderTotal = new OrderTotalViewModel { TotalQuantity = 2, TotalValue = 5.0m };
            _mockOrderService.Setup(o => o.GetOrderTotal(mockCart)).Returns(mockOrderTotal);
            var result = await _controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("canteen-url", _controller.ViewBag.CanteenLink);
            Assert.Equal("bar-url", _controller.ViewBag.BarLink);
            Assert.Equal(15.5m, _controller.ViewBag.UserBalance);
            Assert.Equal("Pedro", _controller.ViewBag.FirstName);
            Assert.Equal("Cliente", _controller.ViewBag.UserRole);
            var cartTotal = Assert.IsType<OrderTotalViewModel>(_controller.ViewBag.CartTotal);
            Assert.Equal(5.0m, cartTotal.TotalValue);
        }

        [Fact]
        public async Task Schedule_ReturnsView_WithScheduleViewModel()
        {
            var mockSchedule = new BarCanteenConfigViewModel
            {
                BarOpeningTime = new TimeSpan(8, 0, 0),
                BarClosingTime = new TimeSpan(20, 0, 0),
                CanteenLunchOpeningTime = new TimeSpan(12, 0, 0),
                CanteenLunchClosingTime = new TimeSpan(14, 0, 0)
            };

            _mockAdminService.Setup(s => s.GetScheduleAsync()).ReturnsAsync(mockSchedule);

            var result = await _controller.Schedule();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<BarCanteenConfigViewModel>(viewResult.Model);

            Assert.Equal(new TimeSpan(8, 0, 0), model.BarOpeningTime);
            Assert.Equal(new TimeSpan(14, 0, 0), model.CanteenLunchClosingTime);
        }

        [Fact]
        public void Privacy_ReturnsView()
        {
            var result = _controller.Privacy();
            Assert.IsType<ViewResult>(result);
        }
    }
}