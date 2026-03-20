using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Moq;
using Projeto_SEGUES.Controllers;
using Projeto_SEGUES.Models.Audit.ViewModels;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using System.Security.Claims;
using Projeto_SEGUES.Areas.Admin.ViewModels;

namespace SeguesTests
{
    public class HomeControllerTests
    {
        private readonly Mock<ILogger<HomeController>> _mockLogger;
        private readonly Mock<IStringLocalizer<AppErrors>> _mockLocalizer;
        private readonly Mock<UserManager<AppUser>> _mockUserManager;
        private readonly Mock<IAdminService> _mockAdminService;
        private readonly Mock<IOrderService> _mockOrderService;
        private readonly HomeController _controller;

        public HomeControllerTests()
        {
            _mockLogger = new Mock<ILogger<HomeController>>();
            _mockLocalizer = new Mock<IStringLocalizer<AppErrors>>();
            _mockAdminService = new Mock<IAdminService>();
            _mockOrderService = new Mock<IOrderService>();

            var store = new Mock<IUserStore<AppUser>>();
            _mockUserManager = new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);

            _controller = new HomeController(_mockLogger.Object, _mockLocalizer.Object, _mockUserManager.Object, _mockAdminService.Object, _mockOrderService.Object);

            var httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        }

        // Ensures the index view populates user dashboard data when authenticated
        [Fact]
        public async Task Index_AuthenticatedUser_ReturnsViewWithDashboardData()
        {
            var user = new AppUser
            {
                Id = "u1",
                FirstName = "Diogo",
                LastName = "Teste",
                Balance = 10.0m,
                Email = "test@test.com",
                BirthDate = DateTime.Now.AddYears(-20),
                Gender = Gender.Male,
                UserCategory = new UserCategory { Name = "Estudante" }
            };

            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "test@test.com") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            _controller.HttpContext.User = new ClaimsPrincipal(identity);

            _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
            _mockUserManager.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Admin" });

            var result = await _controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(10.0m, _controller.ViewBag.UserBalance);
            Assert.Equal("Diogo", _controller.ViewBag.FirstName);
            Assert.Equal("Admin", _controller.ViewBag.UserRole);
        }

        // Returns a simple view when the user is not authenticated
        [Fact]
        public async Task Index_NotAuthenticated_ReturnsView()
        {
            _controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

            var result = await _controller.Index();

            Assert.IsType<ViewResult>(result);
        }

        // Verifies the privacy page returns the correct view
        [Fact]
        public void Privacy_ReturnsView()
        {
            var result = _controller.Privacy();
            Assert.IsType<ViewResult>(result);
        }
/*
        // Ensures all bar and meal service times are correctly loaded into the view
        [Fact]
        public async Task Schedule_ReturnsView_WithAllServiceTimes()
        {
            _mockAdminService.Setup(s => s.GetScheduleAsync()).ReturnsAsync(
                ServiceResult<BarCanteenConfigViewModel>.Ok( "", new BarCanteenConfigViewModel {
                BarOpeningTime = "08:00",
                BarClosingTime = "20:00",
                CanteenLunchOpeningTime = "12:00",
                CanteenLunchClosingTime = "14:00",
                CanteenDinnerOpeningTime = "18:00",
                CanteenDinnerClosingTime = "20:00"
            }));
            
            var result = await _controller.Schedule();

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("08:00", _controller.ViewBag.OpeningTime);
            Assert.Equal("20:00", _controller.ViewBag.ClosingTime);
        }*/

        // Returns the error view containing a valid RequestId for debugging
        [Fact]
        public void Error_ReturnsView_WithErrorViewModel()
        {
            var result = _controller.Error();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ErrorViewModel>(viewResult.Model);
            Assert.NotNull(model.RequestId);
        }
    }
}