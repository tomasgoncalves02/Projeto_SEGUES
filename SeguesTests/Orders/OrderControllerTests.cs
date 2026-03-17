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
    public class OrderControllerTests
    {
        private readonly Mock<UserManager<AppUser>> _mockUserManager;
        private readonly Mock<IAdminService> _mockAdminService;
        private readonly OrderController _controller;

        public OrderControllerTests()
        {
            var store = new Mock<IUserStore<AppUser>>();
            _mockUserManager = new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);
            _mockAdminService = new Mock<IAdminService>();

            _controller = new OrderController(_mockUserManager.Object, _mockAdminService.Object);

            var httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        }


        // Ensures the index view initializes with the authenticated user's balance and bar schedule
        [Fact]
        public async Task Index_UserExists_ReturnsViewWithBagData()
        {
            var user = new AppUser
            {
                Id = "user-1",
                Balance = 25.50m,
                FirstName = "Diogo",
                LastName = "Teste",
                Email = "diogo@test.com",
                BirthDate = DateTime.Now.AddYears(-20),
                Gender = Gender.Male,
                UserCategory = new UserCategory { Name = "Estudante" }
            };

            _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
            _mockAdminService.Setup(s => s.GetOpenBarTimeAsync()).ReturnsAsync(new TimeSpan(8, 0, 0));
            _mockAdminService.Setup(s => s.GetCloseBarTimesAsync()).ReturnsAsync(new TimeSpan(20, 0, 0));

            var result = await _controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(25.50m, _controller.ViewBag.UserBalance);
            Assert.Equal("08:00", _controller.ViewBag.OpeningTime);
            Assert.Equal("20:00", _controller.ViewBag.ClosingTime);
        }


        // Returns a ChallengeResult when the user session is null or invalid
        [Fact]
        public async Task Index_UserNotFound_ReturnsChallenge()
        {
            _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((AppUser)null!);

            var result = await _controller.Index();

            Assert.IsType<ChallengeResult>(result);
        }
    }
}