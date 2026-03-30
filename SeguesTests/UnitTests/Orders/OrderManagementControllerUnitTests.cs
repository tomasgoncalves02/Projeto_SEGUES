using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Projeto_SEGUES.Areas.Order;
using Projeto_SEGUES.Areas.Order.ViewModels;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Order;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using SeguesTests.Helpers;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Projeto_SEGUES.Areas.Order.Controllers;
using Xunit;

namespace SeguesTests.UnitTests.Orders
{
    public class OrderManagementControllerUnitTests
    {
        private readonly Mock<IOrderService> _mockOrderService;
        private readonly Mock<UserManager<AppUser>> _mockUserManager;
        private readonly OrderManagementController _controller;

        public OrderManagementControllerUnitTests()
        {
            _mockOrderService = new Mock<IOrderService>();

            var pedro = MockHelper.CreateValidAppUser("staff-pedro");
            _mockUserManager = MockHelper.MockUserManager(new List<AppUser> { pedro });

            _controller = new OrderManagementController(_mockOrderService.Object, _mockUserManager.Object);

            var services = new ServiceCollection();

            services.AddControllersWithViews();
            services.AddLogging();

            var mockLinkGenerator = new Mock<LinkGenerator>();
            mockLinkGenerator
                .Setup(x => x.GetPathByAddress(
                    It.IsAny<string>(),
                    It.IsAny<RouteValueDictionary>(),
                    It.IsAny<PathString>(),
                    It.IsAny<FragmentString>(),
                    It.IsAny<LinkOptions>()))
                .Returns("/Identity/Account/Login");

            services.AddSingleton(mockLinkGenerator.Object);

            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = services.BuildServiceProvider();
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            _controller.Url = new FakeUrlHelper
            {
                ActionContext = new ActionContext(
            _controller.ControllerContext.HttpContext,
            new RouteData(),                                   
            new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()  
            )
            };
        }

        private void SetupUserIdentity(string id, string role)
        {
            var claims = new[] {
                new Claim(ClaimTypes.NameIdentifier, id),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);

            var user = MockHelper.CreateValidAppUser(id);
            _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        }

        [Fact]
        public async Task Index_ReturnsView_WithUndeliveredOrders()
        {
            _mockOrderService.Setup(s => s.GetUndeliveredOrdersAsync())
                .ReturnsAsync(new List<Order>());

            var result = await _controller.Index();

            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task GetOrdersTable_ReturnsPartialView()
        {
            _mockOrderService.Setup(s => s.GetUndeliveredOrdersAsync())
                .ReturnsAsync(new List<Order>());

            var result = await _controller.GetOrdersTable();

            var partialResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_ManageOrdersTablePartial", partialResult.ViewName);
        }

        [Theory]
        [InlineData("Admin")]
        [InlineData("Employee")]
        public async Task UpdateStatus_ValidRole_ReturnsOk(string role)
        {
            SetupUserIdentity("staff-pedro", role);
            _mockOrderService.Setup(s => s.UpdateOrderStatusAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<AppUser>()))
                .ReturnsAsync(ServiceResult.Ok("Sucesso"));

            var result = await _controller.UpdateStatus(1, (int)OrderStatus.ReadyToDeliver);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task UpdateStatus_ServiceFails_ReturnsBadRequest()
        {
            SetupUserIdentity("staff-pedro", "Admin");
            _mockOrderService.Setup(s => s.UpdateOrderStatusAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<AppUser>()))
                .ReturnsAsync(ServiceResult.Fail("Erro"));

            var result = await _controller.UpdateStatus(1, (int)OrderStatus.ReadyToDeliver);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetOrderDetailsSide_ReturnsChallenge_WhenOrderDoesNotExist()
        {
            _mockOrderService.Setup(s => s.GetOrderByIdAsync(999))
                .ReturnsAsync((Order)null!);

            var result = await _controller.GetOrderDetailsSide(999);

            Assert.IsType<ChallengeResult>(result);
            Assert.True(_controller.Response.Headers.ContainsKey("HX-Redirect"));
            Assert.Equal("/Identity/Account/Login", _controller.Response.Headers["HX-Redirect"]);
        }

        [Fact]
        public async Task ValidateOrderCode_ReturnsOk_WhenCodeIsValid()
        {
            SetupUserIdentity("staff-pedro", "Employee");
            _mockOrderService.Setup(s => s.ValidateOrderCodeAsync(It.IsAny<int>(), "PEDRO123", It.IsAny<AppUser>()))
                .ReturnsAsync(ServiceResult.Ok("Código Aceite"));

            var result = await _controller.ValidateOrderCode(1, "PEDRO123");

            Assert.IsType<OkObjectResult>(result);
        }
    }
}