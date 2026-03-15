using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using Projeto_SEGUES.Areas.Ticket;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Services;
using System.Security.Claims;
using Xunit;

namespace SeguesTests.Tickets
{
    public class TicketControllerTest
    {
        private AppDbContext GetDatabaseContext() => new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        private Mock<UserManager<AppUser>> GetMockUserManager() =>
            new Mock<UserManager<AppUser>>(new Mock<IUserStore<AppUser>>().Object, null, null, null, null, null, null, null, null);

        private async Task<(AppUser currentUser, AppUser recipientUser, Mock<ITicketService> mockService, TicketController controller)> SetupFullEnv(string currentUserId, string recipientEmail, bool sameCategory = true)
        {
            var context = GetDatabaseContext();
            var mockUserMgr = GetMockUserManager();
            var mockService = new Mock<ITicketService>();
            var mockAdminService = new Mock<IAdminService>();

            var controller = new TicketController(mockUserMgr.Object, mockService.Object, context, mockAdminService.Object);

            var catEstudante = new UserCategory { Id = 1, Name = "Estudante" };
            var catProfessor = new UserCategory { Id = 2, Name = "Professor" };

            var currentUser = new AppUser
            {
                Id = currentUserId,
                Email = "pedro@segues.pt",
                FirstName = "Pedro",
                LastName = "Atual",
                UserCategory = catEstudante,
                BirthDate = DateTime.Now.AddYears(-20),
                Gender = Gender.Male,
                Balance = 15m
            };

            var recipientUser = new AppUser
            {
                Id = "u-destinatario",
                Email = recipientEmail,
                FirstName = "Joao",
                LastName = "Recebe",
                UserCategory = sameCategory ? catEstudante : catProfessor,
                BirthDate = DateTime.Now.AddYears(-22),
                Gender = Gender.Male
            };

            context.UserCategory.AddRange(catEstudante, catProfessor);
            context.Users.AddRange(currentUser, recipientUser);
            await context.SaveChangesAsync();

            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, currentUserId) }, "Test");
            var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

            mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(currentUser);
            mockUserMgr.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(currentUserId);
            mockUserMgr.Setup(m => m.GetRolesAsync(currentUser)).ReturnsAsync(new List<string> { "Cliente" });

            return (currentUser, recipientUser, mockService, controller);
        }

        // Ensures the main ticket dashboard correctly displays the user's current balance
        [Fact]
        public async Task Index_ReturnsView_WithUserBalance()
        {
            var (_, _, _, controller) = await SetupFullEnv("u-pedro", "b@b.pt");

            var result = await controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(15m, controller.ViewBag.UserBalance);
        }

        // Verifies that a transfer fails when no tickets are selected, returning an error message
        [Fact]
        public async Task TransferTickets_NoTicketsSelected_ReturnsRedirectWithError()
        {
            var (_, _, _, controller) = await SetupFullEnv("u-pedro", "dest@segues.pt");

            var result = await controller.TransferTickets(new List<string>(), "dest@segues.pt");

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("SendTicket", redirect.ActionName);
            Assert.Contains("error", controller.TempData["SwalData"]?.ToString()?.ToLower());
        }

        // Confirms that a successful ticket transfer invokes the service and notifies the user
        [Fact]
        public async Task TransferTickets_Success_CallsServiceAndRedirects()
        {
            var currentUserId = "u-transfere";
            var (_, _, mockService, controller) = await SetupFullEnv(currentUserId, "dest@segues.pt");
            var tickets = new List<string> { "T1", "T2" };

            mockService.Setup(s => s.TransferTicketsAsync(currentUserId, "dest@segues.pt", tickets))
                .ReturnsAsync(ServiceResult.Ok("Success"));

            var result = await controller.TransferTickets(tickets, "dest@segues.pt");

            Assert.IsType<RedirectToActionResult>(result);
            mockService.Verify(s => s.TransferTicketsAsync(currentUserId, "dest@segues.pt", tickets), Times.Once);
        }

        // Validates that users of the same category are eligible for ticket transfers
        [Fact]
        public async Task CheckTransferEligibility_SameCategory_ReturnsSuccessJson()
        {
            var email = "amigo@segues.pt";
            var (_, _, _, controller) = await SetupFullEnv("u-pedro", email, true);

            var result = await controller.CheckTransferEligibility(email);

            var jsonResult = Assert.IsType<JsonResult>(result);
            var successValue = jsonResult.Value?.GetType().GetProperty("success")?.GetValue(jsonResult.Value, null);
            Assert.Equal(true, successValue);
        }

        // Prevents ticket transfers between different user categories to ensure pricing safety
        [Fact]
        public async Task CheckTransferEligibility_DifferentCategory_ReturnsFailureJson()
        {
            var email = "professor@segues.pt";
            var (_, _, _, controller) = await SetupFullEnv("u-pedro", email, false);

            var result = await controller.CheckTransferEligibility(email);

            var jsonResult = Assert.IsType<JsonResult>(result);
            var successValue = jsonResult.Value?.GetType().GetProperty("success")?.GetValue(jsonResult.Value, null);
            Assert.Equal(false, successValue);
        }

        // Ensures the active tickets list can be refreshed dynamically via an HTMX partial view
        [Fact]
        public async Task GetUpdatedActiveTickets_ReturnsPartialView()
        {
            var (_, _, _, controller) = await SetupFullEnv("u-pedro", "any@test.pt");

            var result = await controller.GetUpdatedActiveTickets();

            var partial = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_ActiveTicketsCards", partial.ViewName);
        }

        // Returns a ChallengeResult if the user session is lost or invalid when accessing the canteen index
        [Fact]
        public async Task Index_UserNotFound_ReturnsChallenge()
        {
            var context = GetDatabaseContext();
            var mockUserMgr = GetMockUserManager();
            var controller = new TicketController(mockUserMgr.Object, Mock.Of<ITicketService>(), context, Mock.Of<IAdminService>());

            mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((AppUser)null!);

            var result = await controller.Index();

            Assert.IsType<ChallengeResult>(result);
        }

        // Ensures the transfer process is aborted with an error message if the recipient email is not provided
        [Fact]
        public async Task TransferTickets_MissingRecipientEmail_ReturnsError()
        {
            var (_, _, _, controller) = await SetupFullEnv("u-pedro", "dest@segues.pt");

            var result = await controller.TransferTickets(new List<string> { "T1" }, "");

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("SendTicket", redirect.ActionName);
            Assert.Contains("error", controller.TempData["SwalData"]?.ToString()?.ToLower());
        }

        // Verifies that the controller correctly handles and displays service-level errors during ticket transfers
        [Fact]
        public async Task TransferTickets_ServiceFailure_ReturnsRedirectWithError()
        {
            var currentUserId = "u-pedro";
            var (_, _, mockService, controller) = await SetupFullEnv(currentUserId, "dest@segues.pt");
            var tickets = new List<string> { "T1" };

            mockService.Setup(s => s.TransferTicketsAsync(currentUserId, "dest@segues.pt", tickets))
                .ReturnsAsync(ServiceResult.Fail("Transfer failed due to system error"));

            var result = await controller.TransferTickets(tickets, "dest@segues.pt");

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Contains("error", controller.TempData["SwalData"]?.ToString()?.ToLower());
        }

        // Confirms that the transfer eligibility check returns a failure JSON if the recipient email does not exist in the system
        [Fact]
        public async Task CheckTransferEligibility_RecipientNotFound_ReturnsFailureJson()
        {
            var (_, _, _, controller) = await SetupFullEnv("u-pedro", "existing@test.pt");

            var result = await controller.CheckTransferEligibility("nonexistent@test.pt");

            var jsonResult = Assert.IsType<JsonResult>(result);
            var successValue = jsonResult.Value?.GetType().GetProperty("success")?.GetValue(jsonResult.Value, null);
            Assert.Equal(false, successValue);
        }

        // Verifies that the general tickets table can be retrieved as a partial view for HTMX updates
        [Fact]
        public async Task GetUpdatedTickets_ReturnsPartialView()
        {
            var (_, _, _, controller) = await SetupFullEnv("u-pedro", "any@test.pt");

            var result = await controller.GetUpdatedTickets();

            var partial = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_TicketTable", partial.ViewName);
        }
    }
}