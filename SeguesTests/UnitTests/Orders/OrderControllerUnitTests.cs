using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Areas.Order;
using Projeto_SEGUES.Areas.Order.ViewModels;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Projeto_SEGUES.Areas.Order.Controllers;
using Xunit;

namespace SeguesTests.UnitTests.Orders
{
    public class OrderControllerUnitTests
    {
        private readonly Mock<UserManager<AppUser>> _mockUserManager;
        private readonly Mock<IAdminService> _mockAdminService;
        private readonly Mock<IOrderService> _mockOrderService;
        private readonly OrderController _controller;

        public OrderControllerUnitTests()
        {
            var store = new Mock<IUserStore<AppUser>>();
            _mockUserManager = new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);
            _mockAdminService = new Mock<IAdminService>();
            _mockOrderService = new Mock<IOrderService>();

            _controller = new OrderController(_mockUserManager.Object, _mockAdminService.Object, _mockOrderService.Object);

            var httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        }

        [Fact]
        public async Task Index_UserExists_ReturnsViewWithModelData()
        {
            var user = new AppUser
            {
                Id = "user-1",
                Balance = 25.50m,
                FirstName = "Pedro",
                LastName = "Teste",
                Email = "pedro@test.com",
                BirthDate = DateTime.Now.AddYears(-20),
                Gender = Gender.Male,
                UserCategory = new UserCategory { Name = "Estudante" }
            };

            var config = new BarCanteenConfigViewModel
            {
                BarOpeningTime = new TimeSpan(8, 0, 0),
                BarClosingTime = new TimeSpan(20, 0, 0),
                BarOpeningTimeString = "08:00",
                BarClosingTimeString = "20:00",
                IsOpenSaturday = true,
                IsOpenSunday = true,
                BarMenuLink = "http://menu.local"
            };

            var cart = new Projeto_SEGUES.Models.Order.Order { AppUser = user, OrderDate = DateTime.Now };
            var cartTotal = new OrderTotalViewModel { TotalQuantity = 2, TotalValue = 5.0m };

            _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
            _mockAdminService.Setup(s => s.GetScheduleAsync()).ReturnsAsync(config);
            _mockOrderService.Setup(s => s.GetCartAsync(user.Id, false)).ReturnsAsync(cart);
            _mockOrderService.Setup(s => s.GetOrderTotal(cart)).Returns(cartTotal);

            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, user.Id) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);

            var result = await _controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<OrderPageViewModel>(viewResult.Model);

            Assert.Contains("25", model.UserBalance);
            Assert.Contains("50", model.UserBalance);
            Assert.Equal("08:00", model.BarOpeningTimeString);
            Assert.Equal("20:00", model.BarClosingTimeString);
            Assert.True(model.IsOpenSaturday);
            Assert.True(model.IsOpenSunday);
            Assert.Equal("http://menu.local", model.BarMenuLink);
            Assert.Equal(cartTotal.TotalQuantity, model.CartTotal.TotalQuantity);
            Assert.Equal(cartTotal.TotalValue, model.CartTotal.TotalValue);
            Assert.False(model.IsClosedByWeekend);

            var now = DateTime.Now.TimeOfDay;
            if (now >= config.BarOpeningTime && now <= config.BarClosingTime)
            {
                Assert.False(model.IsOutsideHours);
            }
        }

        [Fact]
        public async Task Index_UserNotFound_ReturnsChallenge()
        {
            _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((AppUser)null!);

            var result = await _controller.Index();

            Assert.IsType<ChallengeResult>(result);
        }
    }
}