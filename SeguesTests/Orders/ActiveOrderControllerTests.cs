using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Projeto_SEGUES.Areas.Order;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using System.Security.Claims;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Projeto_SEGUES.Resources;

namespace SeguesTests.Orders
{
    public class ActiveOrderControllerTests
    {
        private readonly Mock<UserManager<AppUser>> _mockUserManager;
        private readonly Mock<IOrderService> _mockOrderService;
        private readonly ActiveOrderController _controller;
        private readonly Mock<ILogger<ActiveOrderController>> _mockLogger;
        private readonly Mock<IStringLocalizer<Errors>> _mockLocalizer;

        public ActiveOrderControllerTests()
        {
            var store = new Mock<IUserStore<AppUser>>();
            _mockUserManager = new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);
            _mockOrderService = new Mock<IOrderService>();
            _mockLogger = new Mock<ILogger<ActiveOrderController>>();
            _mockLocalizer = new Mock<IStringLocalizer<Errors>>();

            _controller = new ActiveOrderController(_mockOrderService.Object);

            var httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            _controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        }

        [Fact]
        public async Task Index_ReturnsView_WithActiveOrders()
        {
            var userId = "user-123";
            _mockUserManager.Setup(u => u.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);
            _mockOrderService.Setup(s => s.GetActiveOrdersAsync(userId)).ReturnsAsync(new List<Projeto_SEGUES.Models.Order.Order>());

            var result = await _controller.Index();

            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task GetUpdatedActiveOrders_ReturnsPartialView()
        {
            var userId = "user-123";
            _mockUserManager.Setup(u => u.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);

            var result = await _controller.GetUpdatedActiveOrders();

            var partialResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_ActiveOrdersCards", partialResult.ViewName);
        }

        [Fact]
        public async Task OrderDetails_WrongUser_RedirectsWithError()
        {
            var currentUserId = "user-A";
            var order = new Projeto_SEGUES.Models.Order.Order
            {
                Id = 1,
                AppUser = new AppUser
                {
                    Id = "user-B",
                    BirthDate = DateTime.Now.AddYears(-20),
                    FirstName = "Joao",
                    LastName = "Teste",
                    Email = "joao@test.com",
                    Gender = Gender.Male,
                    UserCategory = new UserCategory { Name = "Estudante" }
                },
                Status = Projeto_SEGUES.Models.Enums.OrderStatus.Pending,
                OrderDate = DateTime.Now
            };

            _mockUserManager.Setup(u => u.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(currentUserId);
            _mockOrderService.Setup(s => s.GetOrderByIdAsync(1)).ReturnsAsync(order);

            var result = await _controller.OrderDetails(1);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.NotNull(_controller.TempData["SwalData"]);
        }

        [Fact]
        public async Task CancelOrder_ServiceSuccess_SetsSuccessMessage()
        {
            _mockOrderService.Setup(s => s.CancelOrderAsync(1))
                .ReturnsAsync(ServiceResult.Ok("Cancelado"));

            var result = await _controller.CancelOrder(1);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Contains("success", _controller.TempData["SwalData"]?.ToString()?.ToLower());
        }

        [Fact]
        public async Task CancelOrder_ServiceFailure_SetsErrorMessage()
        {
            _mockOrderService.Setup(s => s.CancelOrderAsync(1))
                .ReturnsAsync(ServiceResult.Fail("Erro ao cancelar"));

            var result = await _controller.CancelOrder(1);

            Assert.IsType<RedirectToActionResult>(result);
            Assert.Contains("error", _controller.TempData["SwalData"]?.ToString()?.ToLower());
        }
    }
}