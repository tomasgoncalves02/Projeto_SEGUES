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
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Projeto_SEGUES.Resources;

namespace SeguesTests.Admin
{
    public class AdminTicketManagementControllerTests
    {
        private readonly Mock<IAdminService> _mockAdminService;
        private readonly Mock<ITicketService> _mockTicketService;
        private readonly Mock<UserManager<AppUser>> _mockUserManager;
        private readonly AdminTicketManagementController _controller;
        private readonly Mock<ILogger<AdminTicketManagementController>> _mockLogger;
        private readonly Mock<IStringLocalizer<Errors>> _mockLocalizer;

        public AdminTicketManagementControllerTests()
        {
            _mockAdminService = new Mock<IAdminService>();
            _mockTicketService = new Mock<ITicketService>();
            _mockLogger = new Mock<ILogger<AdminTicketManagementController>>();
            _mockLocalizer = new Mock<IStringLocalizer<Errors>>();

            var store = new Mock<IUserStore<AppUser>>();
            _mockUserManager = new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);

            _controller = new AdminTicketManagementController(
                _mockAdminService.Object,
                _mockUserManager.Object,
                _mockTicketService.Object,
                _mockLogger.Object,
                _mockLocalizer.Object
                );

            var httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            _controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        }

        // Helper to create a valid user with required members
        private AppUser CreateValidTestUser() => new()
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@test.com",
            BirthDate = DateTime.Now.AddYears(-20),
            Gender = Gender.Male,
            UserCategory = new UserCategory { Name = "Estudante" }
        };

        // Helper to create a valid purchase
        private TicketPurchase CreateValidPurchase(AppUser user) => new()
        {
            AppUser = user,
            TransactionDate = DateTime.Now,
            Value = 2.50m,
            Quantity = 1
        };


        // Verifies the index view returns current ticket prices and audit history
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


        // Ensures valid price updates redirect to index with a success message
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


        // Handles system exceptions during price updates by showing an error message
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


        // Confirms that global ticket validity can be updated successfully
        [Fact]
        public async Task UpdateValidity_ValidDays_RedirectsWithSuccess()
        {
            int days = 15;

            var result = await _controller.UpdateValidity(days);

            _mockAdminService.Verify(s => s.UpdateTicketValidityDaysAsync(days), Times.Once);
            Assert.IsType<RedirectToActionResult>(result);

            Assert.Contains("success", _controller.TempData["SwalData"]?.ToString());
        }


        // Prevents updating validity days with values less than 1
        [Fact]
        public async Task UpdateValidity_InvalidDays_RedirectsWithError()
        {
            int days = 0;

            var result = await _controller.UpdateValidity(days);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);

            Assert.Contains("error", _controller.TempData["SwalData"]?.ToString());
        }


        // Ensures the audit table partial view is returned for dynamic updates
        [Fact]
        public async Task GetUpdatedAuditTable_ReturnsPartialView()
        {
            // Arrange
            var history = new List<Ticket>();
            _mockTicketService.Setup(s => s.GetAllTicketsAsync()).ReturnsAsync(history);

            // Act
            // Adicionamos o quarto parâmetro (null) correspondente ao flowFilter
            var result = await _controller.GetUpdatedAuditTable(string.Empty, null, null, null);

            // Assert
            var partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_AuditTableRows", partialViewResult.ViewName);

            // Verificamos se o modelo retornado é a lista (o Controller faz .ToList() no final)
            var model = Assert.IsAssignableFrom<IEnumerable<Ticket>>(partialViewResult.Model);
            Assert.Equal(history.Count, model.Count());
        }

        // Redirects to index when no prices are provided for update
        [Fact]
        public async Task UpdatePrices_NullList_RedirectsToIndex()
        {
            var result = await _controller.UpdatePrices(null!);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
        }

        // Redirects after successfully updating service schedules
        [Fact]
        public async Task UpdateSchedule_ValidTimes_RedirectsWithSuccess()
        {
            var open = new TimeSpan(12, 0, 0);
            var close = new TimeSpan(14, 0, 0);

            var result = await _controller.UpdateSchedule("Almoço", open, close);

            //_mockAdminService.Verify(s => s.UpdateBarScheduleAsync(open.ToString(), close.ToString(), "Almoço"), Times.Once);
            Assert.IsType<RedirectToActionResult>(result);
        }

        // Prevents schedules where opening time is after or equal to closing
        [Fact]
        public async Task UpdateSchedule_InvalidTimes_ReturnsError()
        {
            var open = new TimeSpan(15, 0, 0);
            var close = new TimeSpan(14, 0, 0);

            var result = await _controller.UpdateSchedule("Jantar", open, close);

            Assert.IsType<RedirectToActionResult>(result);
            Assert.Contains("error", _controller.TempData.Values.FirstOrDefault()?.ToString()?.ToLower());
        }

        // Verifies the audit table filter logic for specific validation codes
        [Fact]
        public async Task GetUpdatedAuditTable_WithSearchFilter_ReturnsFilteredResults()
        {
            // Arrange
            var user = CreateValidTestUser();
            var purchase = CreateValidPurchase(user);
            var history = new List<Ticket>
    {
        new Ticket { ValidationCode = "MATCH123", Owner = user, TicketPurchase = purchase },
        new Ticket { ValidationCode = "OTHER", Owner = user, TicketPurchase = purchase }
    };
            _mockTicketService.Setup(s => s.GetAllTicketsAsync()).ReturnsAsync(history);

            // Act
            // Adicionamos o 4º parâmetro como null para ignorar o filtro de fluxo neste teste
            var result = await _controller.GetUpdatedAuditTable("MATCH", null, null, null);

            // Assert
            var partialViewResult = Assert.IsType<PartialViewResult>(result);
            var model = Assert.IsAssignableFrom<List<Ticket>>(partialViewResult.Model);

            Assert.Single(model);
            Assert.Equal("MATCH123", model[0].ValidationCode);
        }

        // Ensures the ticket audit report is generated as a PDF file
        [Fact]
        public async Task ExportTicketsPDF_ReturnsFileResult()
        {
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
            _mockTicketService.Setup(s => s.GetAllTicketsAsync()).ReturnsAsync(new List<Ticket>());

            var result = await _controller.ExportTicketsPDF();

            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("application/pdf", fileResult.ContentType);
            Assert.Equal("Auditoria_Senhas_IPS.pdf", fileResult.FileDownloadName);
        }
    }
}