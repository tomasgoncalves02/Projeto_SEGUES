using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Projeto_SEGUES.Areas.Order;
using Projeto_SEGUES.Areas.Order.ViewModels;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using System.Security.Claims;

namespace SeguesTests.UnitTests.Orders
{
    public class ActiveOrderUnitTests
    {
        private readonly Mock<IOrderService> _mockOrderService;
        private readonly ActiveOrderController _controller;

        public ActiveOrderUnitTests()
        {
            _mockOrderService = new Mock<IOrderService>();
            _controller = new ActiveOrderController(_mockOrderService.Object);

            var httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            _controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        }

        private void SetUserContext(string userId, string role = "User")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext.User = claimsPrincipal;
        }

        [Fact]
        public async Task Index_ReturnsView_WithActiveOrders()
        {
            var userId = "pedro-77";
            SetUserContext(userId);
            _mockOrderService.Setup(s => s.GetActiveOrdersAsync(userId)).ReturnsAsync(new List<Projeto_SEGUES.Models.Order.Order>());

            var result = await _controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsAssignableFrom<IEnumerable<Projeto_SEGUES.Models.Order.Order>>(viewResult.Model);
        }

        [Fact]
        public async Task GetUpdatedActiveOrders_ReturnsPartialView()
        {
            var userId = "pedro-77";
            SetUserContext(userId);
            _mockOrderService.Setup(s => s.GetActiveOrdersAsync(userId)).ReturnsAsync(new List<Projeto_SEGUES.Models.Order.Order>());

            var result = await _controller.GetUpdatedActiveOrders();

            var partialResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_ActiveOrdersCardsPartial", partialResult.ViewName);
        }

        [Fact]
        public async Task OrderDetails_WrongUser_RedirectsWithError()
        {
            var currentUserId = "pedro-77";
            SetUserContext(currentUserId);

            var order = new Projeto_SEGUES.Models.Order.Order
            {
                Id = 1,
                AppUser = new AppUser
                {
                    Id = "pedro-invasor",
                    FirstName = "Pedro",
                    LastName = "Invasor",
                    Email = "pedro2@segues.pt",
                    BirthDate = DateTime.Now.AddYears(-20),
                    Gender = Gender.Male,
                    UserCategory = new UserCategory { Name = "Estudante" }
                },
                Status = OrderStatus.Pending,
                OrderDate = DateTime.Now
            };

            _mockOrderService.Setup(s => s.GetOrderByIdAsync(1)).ReturnsAsync(order);

            var result = await _controller.OrderDetails(1);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.NotNull(_controller.TempData["SwalData"]);
            Assert.Contains("error", _controller.TempData["SwalData"]?.ToString()?.ToLower());
        }

        [Fact]
        public async Task CancelOrder_ServiceSuccess_SetsSuccessMessage()
        {
            _mockOrderService.Setup(s => s.CancelOrderAsync(1))
                .ReturnsAsync(ServiceResult.Ok("Cancelado"));

            var result = await _controller.CancelOrder(1);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Contains("success", _controller.TempData["SwalData"]?.ToString()?.ToLower());
        }

        [Fact]
        public async Task CancelOrder_ServiceFailure_SetsErrorMessage()
        {
            _mockOrderService.Setup(s => s.CancelOrderAsync(1))
                .ReturnsAsync(ServiceResult.Fail("Erro ao cancelar"));

            var result = await _controller.CancelOrder(1);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Contains("error", _controller.TempData["SwalData"]?.ToString()?.ToLower());
        }
    }
}