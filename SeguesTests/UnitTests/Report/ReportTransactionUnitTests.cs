using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Projeto_SEGUES.Areas.Report.Controllers;
using Projeto_SEGUES.Areas.Report.ViewModels;
using Projeto_SEGUES.Models.Payment;
using Projeto_SEGUES.Services;
using System.Security.Claims;
using Xunit;

namespace SeguesTests.UnitTests.Report
{
    public class ReportTransactionUnitTests
    {
        private readonly Mock<IReportService> _mockService;
        private readonly ReportTransactionController _controller;

        public ReportTransactionUnitTests()
        {
            _mockService = new Mock<IReportService>();
            _controller = new ReportTransactionController(_mockService.Object);
        }

        private void SetupUser(string userId)
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        [Fact]
        public async Task Index_ReturnsViewWithModel()
        {
            SetupUser("pedro-77");
            var model = new ReportTransactionSearchViewModel();
            _mockService.Setup(s => s.GetTransactionHistoryAsync("pedro-77", model))
                .ReturnsAsync(new List<Transaction>());

            var result = await _controller.Index(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, viewResult.Model);
        }

        [Fact]
        public async Task GetFilteredBalance_ReturnsPartialView()
        {
            SetupUser("pedro-77");
            var model = new ReportTransactionSearchViewModel();
            _mockService.Setup(s => s.GetTransactionHistoryAsync("pedro-77", model))
                .ReturnsAsync(new List<Transaction>());

            var result = await _controller.GetFilteredBalance(model);

            var partialResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_BalanceHistoryRows", partialResult.ViewName);
        }
    }
}