using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using Projeto_SEGUES.Areas.Admin;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;

namespace SeguesTests.Admin
{
    public class AdminOrderManagementControllerTests
    {
        private readonly Mock<IAdminService> _mockAdminService;
        private readonly Mock<IOrderService> _mockOrderService;
        private readonly Mock<UserManager<AppUser>> _mockUserManager;
        private readonly AppDbContext _context;
        private readonly AdminOrderManagementController _controller;

        public AdminOrderManagementControllerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            _mockAdminService = new Mock<IAdminService>();
            _mockOrderService = new Mock<IOrderService>();

            var store = new Mock<IUserStore<AppUser>>();
            _mockUserManager = new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);

            _controller = new AdminOrderManagementController(
                _mockAdminService.Object,
                _mockOrderService.Object,
                _mockUserManager.Object,
                _context);

            var httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            _controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        }

        // Ensures bar schedule update fails if times are equal
        [Fact]
        public async Task UpdateTime_SameTimes_ReturnsError()
        {
            var time = new TimeSpan(10, 0, 0);
            var result = await _controller.UpdateOpenAndCloseTime(time, time);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Contains("error", _controller.TempData.Values.FirstOrDefault()?.ToString()?.ToLower());
        }

        // Ensures bar must be open for at least one hour
        [Fact]
        public async Task UpdateTime_DurationLessThanOneHour_ReturnsError()
        {
            var open = new TimeSpan(10, 0, 0);
            var close = new TimeSpan(10, 30, 0);

            var result = await _controller.UpdateOpenAndCloseTime(open, close);

            Assert.IsType<RedirectToActionResult>(result);
            Assert.Contains("pelo menos 1 hora", _controller.TempData.Values.FirstOrDefault()?.ToString());
        }

        // Redirects to Index after successful schedule update
        [Fact]
        public async Task UpdateTime_ValidTimes_RedirectsWithSuccess()
        {
            var open = new TimeSpan(08, 0, 0);
            var close = new TimeSpan(20, 0, 0);

            var result = await _controller.UpdateOpenAndCloseTime(open, close);

            Assert.IsType<RedirectToActionResult>(result);
            _mockAdminService.Verify(s => s.UpdateBarScheduleAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }
    }
}