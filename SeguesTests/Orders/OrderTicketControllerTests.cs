using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Projeto_SEGUES.Areas.Order;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Ticket;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using System.Security.Claims;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Projeto_SEGUES.Resources;

namespace SeguesTests.Orders
{
    public class OrderTicketControllerTests
    {
        private readonly Mock<ITicketService> _mockTicketService;
        private readonly Mock<UserManager<AppUser>> _mockUserManager;
        private readonly OrderTicketController _controller;
        private readonly Mock<ILogger<OrderTicketController>> _mockLogger;
        private readonly Mock<IStringLocalizer<Errors>> _mockLocalizer;

        public OrderTicketControllerTests()
        {
            _mockTicketService = new Mock<ITicketService>();
            var store = new Mock<IUserStore<AppUser>>();
            _mockUserManager = new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);
            _mockLogger = new Mock<ILogger<OrderTicketController>>();
            _mockLocalizer = new Mock<IStringLocalizer<Errors>>();

            _controller = new OrderTicketController(_mockTicketService.Object, _mockUserManager.Object, _mockLogger.Object, _mockLocalizer.Object);

            var httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            _controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        }

        private AppUser CreateTestUser() => new()
        {
            Id = "user-123",
            FirstName = "Diogo",
            LastName = "User",
            Email = "diogo@test.com",
            BirthDate = DateTime.Now.AddYears(-20),
            Gender = Gender.Male,
            UserCategory = new UserCategory { Name = "Estudante" },
            Balance = 10.00m
        };

        // Ensures the index view loads correctly with user balance, current ticket price, and ticket list
        [Fact]
        public async Task Index_ValidUser_ReturnsViewWithData()
        {
            var user = CreateTestUser();
            _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
            _mockTicketService.Setup(s => s.GetCurrentPriceForUserAsync(user)).ReturnsAsync(2.50m);
            _mockTicketService.Setup(s => s.GetUserTicketsAsync(user.Id)).ReturnsAsync(new List<Ticket>());

            var result = await _controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(10.00m, _controller.ViewBag.UserBalance);
            Assert.Equal(2.50m, _controller.ViewBag.CurrentPrice);
        }

        // Redirects to challenge if the user session is not found during index access
        [Fact]
        public async Task Index_UserNotFound_ReturnsChallenge()
        {
            _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((AppUser)null!);

            var result = await _controller.Index();

            Assert.IsType<ChallengeResult>(result);
        }

        // Verifies successful ticket purchase redirects to index with a success notification
        [Fact]
        public async Task BuyTicket_Success_RedirectsWithSuccessMessage()
        {
            var userId = "user-123";
            _mockUserManager.Setup(u => u.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);
            _mockTicketService.Setup(s => s.BuyTicketsAsync(userId, 1))
                .ReturnsAsync(ServiceResult.Ok("Purchase successful"));

            var result = await _controller.BuyTicket(1);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(OrderTicketController.Index), redirectResult.ActionName);
            Assert.Contains("success", _controller.TempData["SwalData"]?.ToString()?.ToLower());
        }

        // Handles failed purchases (e.g., insufficient balance) by redirecting with an error message
        [Fact]
        public async Task BuyTicket_Failure_RedirectsWithErrorMessage()
        {
            var userId = "user-123";
            _mockUserManager.Setup(u => u.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);
            _mockTicketService.Setup(s => s.BuyTicketsAsync(userId, 1))
                .ReturnsAsync(ServiceResult.Fail("Insufficient balance"));

            var result = await _controller.BuyTicket(1);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Contains("error", _controller.TempData["SwalData"]?.ToString()?.ToLower());
        }
    }
}