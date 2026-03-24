using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Projeto_SEGUES.Areas.Ticket;
using Projeto_SEGUES.Areas.Ticket.ViewModels;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using System.Security.Claims;

namespace SeguesTests.Tickets
{
    public class TicketValidationControllerTests
    {
        private readonly Mock<ITicketService> _mockTicketService;
        private readonly Mock<UserManager<AppUser>> _mockUserManager;
        private readonly TicketValidationController _controller;

        public TicketValidationControllerTests()
        {
            _mockTicketService = new Mock<ITicketService>();
            var store = new Mock<IUserStore<AppUser>>();
            _mockUserManager = new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);

            _controller = new TicketValidationController(_mockUserManager.Object, _mockTicketService.Object);

            var httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            _controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        }

        private AppUser CreatePedroEmployee() => new()
        {
            FirstName = "Pedro",
            LastName = "Staff",
            BirthDate = DateTime.Today.AddYears(-20),
            Gender = Projeto_SEGUES.Models.Enums.Gender.Male,
            UserCategory = new UserCategory { Name = "Employee" },
            Email = "pedro.staff@segues.pt"
        };

        // Confirms that the validation dashboard loads with the history of recently used tickets
        [Fact]
        public async Task Index_Get_ReturnsViewWithRecentTickets()
        {
            _mockTicketService.Setup(s => s.GetRecentUsedTicketsAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<Projeto_SEGUES.Models.Ticket.Ticket>());

            var result = await _controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ValidateTicketViewModel>(viewResult.Model);
            Assert.NotNull(model.RecentTickets);
        }

        // Ensures that validation is aborted and the view is returned if the ticket code is missing
        [Fact]
        public async Task Index_Post_InvalidModelState_ReturnsViewWithTickets()
        {
            var model = new ValidateTicketViewModel { Code = "" };
            _controller.ModelState.AddModelError("Code", "Required");

            _mockTicketService.Setup(s => s.GetRecentUsedTicketsAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<Projeto_SEGUES.Models.Ticket.Ticket>());

            var result = await _controller.Index(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            _mockTicketService.Verify(s => s.ValidateTicketAsync(It.IsAny<string>(), It.IsAny<AppUser>()), Times.Never);
        }

        // Verifies that a valid ticket code is successfully processed and a success notification is set
        [Fact]
        public async Task Index_Post_SuccessfulValidation_SetsSuccessMessage()
        {
            var pedro = CreatePedroEmployee();
            var model = new ValidateTicketViewModel { Code = "VALID123" };

            _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(pedro);
            _mockTicketService.Setup(s => s.ValidateTicketAsync("VALID123", pedro))
                .ReturnsAsync(ServiceResult.Ok("Ticket validated"));

            _mockTicketService.Setup(s => s.GetRecentUsedTicketsAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<Projeto_SEGUES.Models.Ticket.Ticket>());

            var result = await _controller.Index(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Contains("success", _controller.TempData["SwalData"]?.ToString()?.ToLower());
            Assert.Empty(model.Code);
        }

        // Confirms that the controller correctly handles ticket rejection and displays an error message to the staff
        [Fact]
        public async Task Index_Post_FailedValidation_SetsErrorMessage()
        {
            var pedro = CreatePedroEmployee();
            var model = new ValidateTicketViewModel { Code = "EXPIRED" };

            _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(pedro);
            _mockTicketService.Setup(s => s.ValidateTicketAsync("EXPIRED", pedro))
                .ReturnsAsync(ServiceResult.Fail("Ticket already used"));

            _mockTicketService.Setup(s => s.GetRecentUsedTicketsAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<Projeto_SEGUES.Models.Ticket.Ticket>());

            await _controller.Index(model);

            Assert.Contains("error", _controller.TempData["SwalData"]?.ToString()?.ToLower());
        }

        // Returns a ChallengeResult if the staff member's session expires or is invalid during ticket validation
        [Fact]
        public async Task Index_Post_UserNotFound_ReturnsChallenge()
        {
            _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((AppUser)null!);

            var result = await _controller.Index(new ValidateTicketViewModel { Code = "ANY" });

            Assert.IsType<ChallengeResult>(result);
        }

        // Confirms that the ModelState is cleared after a successful validation to prepare the form for the next entry
        [Fact]
        public async Task Index_Post_Success_ClearsModelState()
        {
            var pedro = CreatePedroEmployee();
            var model = new ValidateTicketViewModel { Code = "TICKET-OK" };

            _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(pedro);
            _mockTicketService.Setup(s => s.ValidateTicketAsync(It.IsAny<string>(), It.IsAny<AppUser>()))
                .ReturnsAsync(ServiceResult.Ok("Success"));
            _mockTicketService.Setup(s => s.GetRecentUsedTicketsAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<Projeto_SEGUES.Models.Ticket.Ticket>());

            await _controller.Index(model);

            Assert.True(_controller.ModelState.IsValid);
            Assert.Empty(_controller.ModelState);
        }

        // Ensures that the recent tickets list is still refreshed even if the submitted validation code is null or empty
        [Fact]
        public async Task Index_Post_InvalidModel_StillRefreshesRecentTickets()
        {
            var model = new ValidateTicketViewModel { Code = "" };
            _controller.ModelState.AddModelError("Code", "Required");

            _mockTicketService.Setup(s => s.GetRecentUsedTicketsAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<Projeto_SEGUES.Models.Ticket.Ticket>());

            await _controller.Index(model);

            _mockTicketService.Verify(s => s.GetRecentUsedTicketsAsync(It.IsAny<int>()), Times.Once);
        }
    }
}