using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Projeto_SEGUES.Areas.Report;
using Projeto_SEGUES.Areas.Report.ViewModels;
using Projeto_SEGUES.Models.Order;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Projeto_SEGUES.Areas.Report.Controllers;
using Xunit;

namespace SeguesTests.UnitTests.Report
{
    public class ReportOrderUnitTests
    {
        private readonly Mock<IReportService> _mockReportService;
        private readonly Mock<IOrderService> _mockOrderService;
        private readonly ReportOrderController _controller;

        public ReportOrderUnitTests()
        {
            _mockReportService = new Mock<IReportService>();
            _mockOrderService = new Mock<IOrderService>();
            _controller = new ReportOrderController(_mockReportService.Object, _mockOrderService.Object);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "pedro-77")
            }, "Test"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async Task Index_ReturnsViewWithModel()
        {
            var model = new ReportOrderSearchViewModel();
            _mockReportService.Setup(s => s.GetOrderHistoryAsync("pedro-77", model))
                .ReturnsAsync(new List<Order>());

            var result = await _controller.Index(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, viewResult.Model);
        }

        [Fact]
        public async Task GetOrderDetails_StrangerUser_ReturnsFailMessage()
        {
            var strangerOrder = new Order
            {
                Id = 1,
                AppUser = new AppUser
                {
                    Id = "outro-utilizador",
                    FirstName = "Joao",
                    LastName = "Silva",
                    BirthDate = System.DateTime.Now,
                    UserCategory = new UserCategory { Name = "X" },
                    Gender = Projeto_SEGUES.Models.Enums.Gender.Male
                }
            };

            _mockOrderService.Setup(s => s.GetOrderByIdAsync(1)).ReturnsAsync(strangerOrder);

            var result = await _controller.GetOrderDetails(1);

            var jsonResult = Assert.IsType<JsonResult>(result);
            var data = jsonResult.Value;
            var failMessage = data?.GetType().GetProperty("failMessage")?.GetValue(data, null);
            Assert.NotNull(failMessage);
        }
    }
}