using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Projeto_SEGUES.Areas.Report;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Order;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using System.Security.Claims;

namespace SeguesTests.Report
{
    public class ReportOrderControllerTests
    {
        private readonly Mock<IOrderService> _mockOrderService;
        private readonly Mock<UserManager<AppUser>> _mockUserManager;
        private readonly ReportOrderController _controller;

        public ReportOrderControllerTests()
        {
            _mockOrderService = new Mock<IOrderService>();
            var store = new Mock<IUserStore<AppUser>>();
            _mockUserManager = new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);

            _controller = new ReportOrderController(_mockOrderService.Object, _mockUserManager.Object);

            var httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        }

        private AppUser CreateValidTestUser(string id) => new()
        {
            Id = id,
            FirstName = "Test",
            LastName = "User",
            Email = "test@test.com",
            BirthDate = DateTime.Now.AddYears(-20),
            Gender = Gender.Male,
            UserCategory = new UserCategory { Name = "Estudante" }
        };


        // Redirects to the login page when an unauthenticated user attempts to access order history
        [Fact]
        public async Task Index_UserNotLoggedIn_RedirectsToLogin()
        {
            _mockUserManager.Setup(u => u.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns((string)null!);

            var result = await _controller.Index();

            var redirectResult = Assert.IsType<RedirectToPageResult>(result);
            Assert.Equal("/Account/Login", redirectResult.PageName);
        }


        // Ensures the index view returns the full order history for the authenticated user
        [Fact]
        public async Task Index_AuthenticatedUser_ReturnsViewWithHistory()
        {
            var user = CreateValidTestUser("user-123");
            var history = new List<Order> { new Order { Id = 1, AppUser = user, OrderDate = DateTime.Now } };

            _mockUserManager.Setup(u => u.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(user.Id);
            _mockOrderService.Setup(s => s.GetOrderHistoryAsync(user.Id)).ReturnsAsync(history);

            var result = await _controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(history, viewResult.Model);
        }


        // Verifies that order details are not returned if the order belongs to a different user (Security Check)
        [Fact]
        public async Task GetOrderDetails_UnauthorizedUser_ReturnsNullData()
        {
            var currentUserId = "user-A";
            var order = new Order
            {
                Id = 1,
                AppUser = CreateValidTestUser("user-B"),
                Status = OrderStatus.Pending
            };

            _mockUserManager.Setup(u => u.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(currentUserId);
            _mockOrderService.Setup(s => s.GetOrderByIdAsync(1)).ReturnsAsync(order);

            var result = await _controller.GetOrderDetails(1);

            var jsonResult = Assert.IsType<JsonResult>(result);
            var data = jsonResult.Value;
            var codigo = data?.GetType().GetProperty("codigo")?.GetValue(data, null);
            Assert.Null(codigo);
        }


        // Confirms that valid order details are correctly serialized and returned as JSON
        [Fact]
        public async Task GetOrderDetails_ValidRequest_ReturnsProductListAsJson()
        {
            var user = CreateValidTestUser("user-123");
            var order = new Order
            {
                Id = 1,
                AppUser = user,
                RedemptionCode = "CODE123",
                ProductPurchases = new List<OrderLine>()
            };

            _mockUserManager.Setup(u => u.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(user.Id);
            _mockOrderService.Setup(s => s.GetOrderByIdAsync(1)).ReturnsAsync(order);

            var result = await _controller.GetOrderDetails(1);

            var jsonResult = Assert.IsType<JsonResult>(result);
            var data = jsonResult.Value;
            var codigo = data?.GetType().GetProperty("codigo")?.GetValue(data, null);
            Assert.Equal("CODE123", codigo);
        }

        // Ensures that requesting a non-existent order ID returns a JSON with null properties
        [Fact]
        public async Task GetOrderDetails_OrderNotFound_ReturnsNullJsonProperties()
        {
            var userId = "user-123";
            _mockUserManager.Setup(u => u.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);
            _mockOrderService.Setup(s => s.GetOrderByIdAsync(999)).ReturnsAsync((Order)null!);

            var result = await _controller.GetOrderDetails(999);

            var jsonResult = Assert.IsType<JsonResult>(result);
            var data = jsonResult.Value;
            var codigo = data?.GetType().GetProperty("codigo")?.GetValue(data, null);
            var produtos = data?.GetType().GetProperty("produtos")?.GetValue(data, null);

            Assert.Null(codigo);
            Assert.Null(produtos);
        }

        // Verifies that the JSON response correctly maps product names and quantities from the order
        [Fact]
        public async Task GetOrderDetails_WithProducts_ReturnsMappedProductList()
        {
            var userId = "user-123";
            var user = CreateValidTestUser(userId);

            var order = new Order
            {
                Id = 1,
                AppUser = user,
                RedemptionCode = "ABC12345",
                Status = OrderStatus.Pending
            };

            var product = new Projeto_SEGUES.Models.Inventory.Product
            {
                Id = 50,
                Description = "sabe muito bem",
                Category = new Projeto_SEGUES.Models.Inventory.ProductCategory { Description = "comeres", Name = "comer" },
                MinimumStock = 10,
                Stock = 60,
                Name = "Café",
                Price = 0.70m
            };

            var orderLine = new OrderLine
            {
                ProductId = product.Id,
                OrderId = order.Id,
                Order = order,
                Product = product,
                ProductValue = product.Price,
                Quantity = 2
            };

            order.ProductPurchases.Add(orderLine);

            _mockUserManager.Setup(u => u.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);
            _mockOrderService.Setup(s => s.GetOrderByIdAsync(1)).ReturnsAsync(order);

            var result = await _controller.GetOrderDetails(1);

            var jsonResult = Assert.IsType<JsonResult>(result);
            var data = jsonResult.Value;

            var produtos = data?.GetType().GetProperty("produtos")?.GetValue(data, null) as IEnumerable<object>;
            var codigo = data?.GetType().GetProperty("codigo")?.GetValue(data, null) as string;

            Assert.NotNull(produtos);
            Assert.Single(produtos);
            Assert.Equal("ABC12345", codigo);
        }
    }
}