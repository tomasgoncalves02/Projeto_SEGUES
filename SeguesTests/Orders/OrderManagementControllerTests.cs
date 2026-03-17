using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Projeto_SEGUES.Areas.Order;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using System.Security.Claims;

namespace SeguesTests.Orders
{
    public class OrderManagementControllerTests
    {
        private readonly Mock<IOrderService> _mockOrderService;
        private readonly Mock<UserManager<AppUser>> _mockUserManager;
        private readonly OrderManagementController _controller;

        public OrderManagementControllerTests()
        {
            _mockOrderService = new Mock<IOrderService>();
            var store = new Mock<IUserStore<AppUser>>();
            _mockUserManager = new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);

            _controller = new OrderManagementController(_mockOrderService.Object, _mockUserManager.Object);

            var httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        }

        private AppUser CreateStaffUser() => new()
        {
            Id = "staff-1",
            FirstName = "Funcionario",
            LastName = "SEGUES",
            Email = "staff@test.com",
            BirthDate = DateTime.Now.AddYears(-25),
            Gender = Gender.Male,
            UserCategory = new UserCategory { Name = "Staff" }
        };

        // Verifies the index view displays all orders currently awaiting delivery
        [Fact]
        public async Task Index_ReturnsView_WithUndeliveredOrders()
        {
            _mockOrderService.Setup(s => s.GetUndeliveredOrdersAsync()).ReturnsAsync(new List<Projeto_SEGUES.Models.Order.Order>());

            var result = await _controller.Index();

            Assert.IsType<ViewResult>(result);
        }

        // Ensures the orders table is returned as a partial view for dynamic updates
        [Fact]
        public async Task GetOrdersTable_ReturnsPartialView()
        {
            _mockOrderService.Setup(s => s.GetUndeliveredOrdersAsync()).ReturnsAsync(new List<Projeto_SEGUES.Models.Order.Order>());

            var result = await _controller.GetOrdersTable();

            var partialResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_ManageOrdersTablePartial", partialResult.ViewName);
        }

        // Confirms that valid order status updates return a success status code
        [Fact]
        public async Task UpdateStatus_Success_ReturnsOk()
        {
            var staff = CreateStaffUser();
            _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(staff);
            _mockOrderService.Setup(s => s.UpdateOrderStatusAsync(1, (int)OrderStatus.Delivered, staff))
                .ReturnsAsync(ServiceResult.Ok());

            var result = await _controller.UpdateStatus(1, (int)OrderStatus.Delivered);

            Assert.IsType<OkResult>(result);
        }

        // Validates the order redemption code and triggers the HX-Trigger header for UI updates
        [Fact]
        public async Task ValidateOrderCode_CorrectCode_ReturnsOkWithHxHeader()
        {
            var staff = CreateStaffUser();
            _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(staff);
            _mockOrderService.Setup(s => s.ValidateOrderCodeAsync(1, "ABC12345", staff))
                .ReturnsAsync(ServiceResult.Ok("Code Validated"));

            var result = await _controller.ValidateOrderCode(1, "ABC12345");

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.True(_controller.Response.Headers.ContainsKey("HX-Trigger"));
            Assert.Equal("orderUpdated", _controller.Response.Headers["HX-Trigger"]);
        }

        // Returns a BadRequest when an incorrect redemption code is entered by staff
        [Fact]
        public async Task ValidateOrderCode_WrongCode_ReturnsBadRequest()
        {
            var staff = CreateStaffUser();
            _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(staff);
            _mockOrderService.Setup(s => s.ValidateOrderCodeAsync(1, "WRONG", staff))
                .ReturnsAsync(ServiceResult.Fail("Invalid Code"));

            var result = await _controller.ValidateOrderCode(1, "WRONG");

            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}