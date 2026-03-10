using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Projeto_SEGUES.Areas.Admin;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Ticket;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using System.Security.Claims;
using Xunit;

namespace SeguesTests.Admin
{
    public class AdminTicketManagementControllerTests
    {
        private readonly Mock<IAdminService> _mockAdminService;
        private readonly Mock<ITicketService> _mockTicketService;
        private readonly Mock<UserManager<AppUser>> _mockUserManager;
        private readonly AdminTicketManagementController _controller;

        public AdminTicketManagementControllerTests()
        {
            _mockAdminService = new Mock<IAdminService>();
            _mockTicketService = new Mock<ITicketService>();

            var store = new Mock<IUserStore<AppUser>>();
            _mockUserManager = new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);

            _controller = new AdminTicketManagementController(
                _mockAdminService.Object,
                _mockUserManager.Object,
                _mockTicketService.Object);

            var httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            _controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        }

        [Fact]
        public async Task Index_ReturnsView_WithPricesAndHistory()
        {
            var userCategory = new UserCategory { Name = "Estudante" };

            var appUser = new AppUser
            {
                FirstName = "Diogo",
                LastName = "Teste",
                UserCategory = userCategory,
                BirthDate = DateTime.Now.AddYears(-20),
                Gender = Gender.Male,
                Email = "diogo@test.com",
                UserName = "diogo@test.com"
            };

            var prices = new List<TicketPrice>
    {
        new TicketPrice
        {
            Price = 2.50m,
            UserCategory = userCategory
        }
    };

            var purchase = new TicketPurchase
            {
                AppUser = appUser,
                TransactionDate = DateTime.Now,
                Value = 2.50m,
                Quantity = 1
            };

            var history = new List<Ticket>
    {
        new Ticket
        {
            ValidationCode = "TEST01",
            Owner = appUser,
            TicketPurchase = purchase,
            ExpirationDate = DateTime.Now.AddDays(30)
        }
    };

            _mockAdminService.Setup(s => s.GetTicketPricesAsync()).ReturnsAsync(prices);
            _mockAdminService.Setup(s => s.GetTicketValidityDaysAsync()).ReturnsAsync(30);
            _mockTicketService.Setup(s => s.GetAllTicketsAsync()).ReturnsAsync(history);
            _mockUserManager.Setup(u => u.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("admin-id");

            var result = await _controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(prices, _controller.ViewBag.Prices);
            Assert.Equal(30, _controller.ViewBag.CurrentValidityDays);
            Assert.Equal(history, viewResult.Model);
        }

        [Fact]
        public async Task UpdatePrices_ValidList_RedirectsWithSuccess()
        {
            var updatedPrices = new List<TicketPrice>
    {
        new TicketPrice
        { 
            Price = 3.00m, 
            UserCategory = new UserCategory { Name = "Estudante" },
            InitialDatePrice = DateTime.Now,
            EndDatePrice = DateTime.Now.AddMonths(1)
        }
    };

            var result = await _controller.UpdatePrices(updatedPrices);

            _mockAdminService.Verify(s => s.UpdateTicketPricesAsync(updatedPrices), Times.Once);
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Contains("success", _controller.TempData["SwalData"]?.ToString());
        }

        [Fact]
        public async Task UpdatePrices_Exception_RedirectsWithError()
        {
            var updatedPrices = new List<TicketPrice>();
            _mockAdminService.Setup(s => s.UpdateTicketPricesAsync(It.IsAny<List<TicketPrice>>()))
                .ThrowsAsync(new System.Exception());

            var itemParaFalhar = new TicketPrice
            {
                Price = 1.00m,
                UserCategory = new UserCategory { Name = "Temp" }
            };

            var result = await _controller.UpdatePrices(new List<TicketPrice> { itemParaFalhar });

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Contains("error", _controller.TempData["SwalData"]?.ToString());
        }

        [Fact]
        public async Task UpdateValidity_ValidDays_RedirectsWithSuccess()
        {
            int days = 15;

            var result = await _controller.UpdateValidity(days);

            _mockAdminService.Verify(s => s.UpdateTicketValidityDaysAsync(days), Times.Once);
            Assert.IsType<RedirectToActionResult>(result);

            Assert.Contains("success", _controller.TempData["SwalData"]?.ToString());
        }

        [Fact]
        public async Task UpdateValidity_InvalidDays_RedirectsWithError()
        {
            int days = 0;

            var result = await _controller.UpdateValidity(days);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);

            Assert.Contains("error", _controller.TempData["SwalData"]?.ToString());
        }

        [Fact]
        public async Task GetUpdatedAuditTable_ReturnsPartialView()
        {
            var history = new List<Ticket>();
            _mockTicketService.Setup(s => s.GetAllTicketsAsync()).ReturnsAsync(history);

            var result = await _controller.GetUpdatedAuditTable("", null, null);

            var partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_AuditTableRows", partialViewResult.ViewName);
            Assert.Equal(history, partialViewResult.Model);
        }
    }
}