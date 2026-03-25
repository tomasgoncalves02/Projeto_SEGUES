using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Projeto_SEGUES.Areas.Order;
using Projeto_SEGUES.Areas.Order.ViewModels;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using System.Security.Claims;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Projeto_SEGUES.Resources;

namespace SeguesTests.Orders
{
    public class CreateOrderControllerTests
    {
        private readonly Mock<IInventoryService> _mockInventoryService;
        private readonly Mock<IOrderService> _mockOrderService;
        private readonly Mock<IAdminService> _mockAdminService;
        private readonly Mock<UserManager<AppUser>> _mockUserManager;
        private readonly CreateOrderController _controller;
        private readonly Mock<ILogger<CreateOrderController>> _mockLogger;
        private readonly Mock<IStringLocalizer<Errors>> _mockLocalizer;

        public CreateOrderControllerTests()
        {
            _mockInventoryService = new Mock<IInventoryService>();
            _mockOrderService = new Mock<IOrderService>();
            _mockAdminService = new Mock<IAdminService>();
            _mockLogger = new Mock<ILogger<CreateOrderController>>();
            _mockLocalizer = new Mock<IStringLocalizer<Errors>>();

            var store = new Mock<IUserStore<AppUser>>();
            _mockUserManager = new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);

            _controller = new CreateOrderController(
                _mockInventoryService.Object,
                _mockOrderService.Object,
                _mockUserManager.Object);

            var httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            _controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        }

        private AppUser CreateValidTestUser() => new()
        {
            Id = "user-1",
            FirstName = "Diogo",
            LastName = "User",
            Email = "diogo@test.com",
            BirthDate = DateTime.Now.AddYears(-20),
            Gender = Gender.Male,
            UserCategory = new UserCategory { Name = "Estudante" },
            Balance = 50.00m
        };

        // Verifies that Index returns available products and populates the cart total
        [Fact]
        public async Task Index_ReturnsView_WithProductsAndCartTotal()
        {
            var user = CreateValidTestUser();
            var cart = new Projeto_SEGUES.Models.Order.Order { AppUser = user, OrderDate = DateTime.Now };

            _mockUserManager.Setup(u => u.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(user.Id);
            _mockOrderService.Setup(s => s.GetCartAsync(user.Id, true)).ReturnsAsync(cart);
            _mockOrderService.Setup(s => s.GetOrderTotal(cart)).Returns(new OrderTotalViewModel());
            _mockInventoryService.Setup(s => s.GetAvailableProductsAsync()).ReturnsAsync(new List<Projeto_SEGUES.Models.Inventory.Product>());

            var result = await _controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(_controller.ViewBag.CartTotal);
        }

        // Ensures adding an item to the cart returns a JSON response with updated totals
        [Fact]
        public async Task AddToCart_ValidProduct_ReturnsJsonResponse()
        {
            var userId = "user-1";
            var totals = new OrderTotalViewModel { TotalQuantity = 2, TotalValue = 5.50m };

            _mockUserManager.Setup(u => u.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);
            // _mockOrderService.Setup(s => s.AddToCartAsync(userId, 1, 1))
            //     .ReturnsAsync(ServiceResult.Ok("Added", totals));

            var result = await _controller.AddToCart(1, 1);

            var jsonResult = Assert.IsType<JsonResult>(result);
            var data = jsonResult.Value;

            var success = (bool)data.GetType().GetProperty("success").GetValue(data, null);
            var count = (int)data.GetType().GetProperty("count").GetValue(data, null);

            Assert.True(success);
            Assert.Equal(2, count);
        }

        // Confirms the checkout view displays the current cart and user balance
        [Fact]
        public async Task Checkout_ReturnsView_WithCartAndBalance()
        {
            var user = CreateValidTestUser();
            var cart = new Projeto_SEGUES.Models.Order.Order { AppUser = user, OrderDate = DateTime.Now };

            _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
            _mockOrderService.Setup(s => s.GetCartAsync(user.Id, true)).ReturnsAsync(cart);
            _mockOrderService.Setup(s => s.GetOrderTotal(cart)).Returns(new OrderTotalViewModel());

            var result = await _controller.Checkout();

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(user.Balance, _controller.ViewBag.Balance);
        }

        // Redirects to active orders after successfully submitting an order
        [Fact]
        public async Task SubmitOrder_Success_RedirectsToActiveOrders()
        {
            var user = CreateValidTestUser();
            _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
            _mockOrderService.Setup(s => s.SubmitOrderAsync(user, true, null))
                .ReturnsAsync(ServiceResult.Ok("Order submitted"));

            var result = await _controller.SubmitOrder(true, null);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("ActiveOrder", redirectResult.ControllerName);
            Assert.Contains("success", _controller.TempData.Values.FirstOrDefault()?.ToString()?.ToLower());
        }

        // Ensures failure to submit an order redirects back to checkout with an error
        [Fact]
        public async Task SubmitOrder_InsufficientBalance_RedirectsToCheckout()
        {
            var user = CreateValidTestUser();
            _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
            _mockOrderService.Setup(s => s.SubmitOrderAsync(user, true, null))
                .ReturnsAsync(ServiceResult.Fail("Saldo insuficiente"));

            var result = await _controller.SubmitOrder(true, null);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Checkout", redirectResult.ActionName);
            Assert.Contains("error", _controller.TempData.Values.FirstOrDefault()?.ToString()?.ToLower());
        }
    }
}