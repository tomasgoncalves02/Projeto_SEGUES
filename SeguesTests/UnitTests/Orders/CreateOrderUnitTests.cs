using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Projeto_SEGUES.Areas.Order.ViewModels;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using System.Security.Claims;
using Projeto_SEGUES.Areas.Order.Controllers;

namespace SeguesTests.UnitTests.Orders;

public class CreateOrderUnitTests
{
    private readonly Mock<IInventoryService> _mockInventoryService;
    private readonly Mock<IOrderService> _mockOrderService;
    private readonly Mock<UserManager<AppUser>> _mockUserManager;
    private readonly CreateOrderController _controller;

    public CreateOrderUnitTests()
    {
        _mockInventoryService = new Mock<IInventoryService>();
        _mockOrderService = new Mock<IOrderService>();
        var mockAdminService = new Mock<IAdminService>();

        var store = new Mock<IUserStore<AppUser>>();
        _mockUserManager = new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);

        _controller = new CreateOrderController(
            _mockInventoryService.Object,
            _mockOrderService.Object,
            _mockUserManager.Object,
            mockAdminService.Object);

        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        _controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        mockAdminService.Setup(s => s.IsBarOpenAsync(It.IsAny<TimeSpan>())).ReturnsAsync(true);
    }

    private static AppUser CreateValidTestUser() => new()
    {
        Id = "user-1",
        FirstName = "Pedro",
        LastName = "Comprador",
        Email = "pedro.compras@segues.pt",
        BirthDate = DateTime.Now.AddYears(-20),
        Gender = Projeto_SEGUES.Models.Enums.Gender.Male,
        UserCategory = new UserCategory { Name = "Estudante" },
        Balance = 50.00m
    };

    private void SetUserContext(string userId, string role = "User")
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _controller.ControllerContext.HttpContext.User = claimsPrincipal;
    }

    [Fact]
    public async Task Index_ReturnsView_WithProductsAndCartTotal()
    {
        var user = CreateValidTestUser();
        SetUserContext(user.Id);
        var cart = new Projeto_SEGUES.Models.Order.Order { AppUser = user, OrderDate = DateTime.Now };

        _mockOrderService.Setup(s => s.GetCartAsync(user.Id, It.IsAny<bool>())).ReturnsAsync(cart);
        _mockOrderService.Setup(s => s.GetOrderTotal(cart)).Returns(new OrderTotalViewModel { TotalQuantity = 1, TotalValue = 1.00m });
        _mockInventoryService.Setup(s => s.GetAvailableProductsAsync()).ReturnsAsync([]);
        _mockInventoryService.Setup(s => s.GetAllCategoriesForDropdownAsync()).ReturnsAsync([]);

        var result = await _controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<CreateOrderViewModel>(viewResult.Model);
    }

    [Fact]
    public async Task AddToCart_ValidProduct_ReturnsOkObjectResult()
    {
        const string userId = "user-1";
        SetUserContext(userId);
        var totals = new OrderTotalViewModel { TotalQuantity = 2, TotalValue = 5.50m };

        _mockOrderService.Setup(s => s.AddToCartAsync(userId, 1, 1))
            .ReturnsAsync(ServiceResult<OrderTotalViewModel>.Ok("Added", totals));

        var result = await _controller.AddToCart(1, 1);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var data = okResult.Value;
        var count = (int?) data?.GetType().GetProperty("count")?.GetValue(data, null);

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task Checkout_ReturnsView_WithCartAndBalance()
    {
        var user = CreateValidTestUser();
        SetUserContext(user.Id);
        var cart = new Projeto_SEGUES.Models.Order.Order { AppUser = user, OrderDate = DateTime.Now };

        _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        _mockOrderService.Setup(s => s.GetCartAsync(user.Id, It.IsAny<bool>())).ReturnsAsync(cart);
        _mockOrderService.Setup(s => s.GetOrderTotal(cart)).Returns(new OrderTotalViewModel { TotalQuantity = 3 });

        var result = await _controller.Checkout();

        Assert.IsType<ViewResult>(result);
        Assert.Equal(user.Balance, _controller.ViewBag.Balance);
    }

    [Fact]
    public async Task SubmitOrder_Success_RedirectsToActiveOrders()
    {
        var user = CreateValidTestUser();
        _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        _mockOrderService.Setup(s => s.SubmitOrderAsync(user, true, null))
            .ReturnsAsync(ServiceResult.Ok("Order submitted"));

        var result = await _controller.SubmitOrder(true, null);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("ActiveOrder", redirectResult.ControllerName);
    }
}