using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using Projeto_SEGUES.Areas.Ticket;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.User;
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

        private Mock<RoleManager<Role>> GetMockRoleManager() =>
            new Mock<RoleManager<Role>>(new Mock<IRoleStore<Role>>().Object, null, null, null, null);

        private async Task<(AppUser currentUser, AppUser recipientUser, Mock<ITicketService> mockService, TicketController controller)> SetupFullEnv(string currentUserId, string recipientEmail)
        {
            var context = GetDatabaseContext();
            var mockUserMgr = GetMockUserManager();
            var mockRoleMgr = GetMockRoleManager();
            var mockService = new Mock<ITicketService>();

            var controller = new TicketController(mockUserMgr.Object, mockService.Object, context);

            var catEstudante = new UserCategory { Name = "Estudante" };
            var catProfessor = new UserCategory { Name = "Professor" };

            var currentUser = new AppUser
            {
                Id = currentUserId,
                Email = "atual@segues.pt",
                FirstName = "Diogo",
                LastName = "Atual",
                UserCategory = catEstudante,
                BirthDate = new DateTime(2000, 1, 1),
                Gender = Projeto_SEGUES.Models.Enums.Gender.Other,
                Balance = 15m
            };

            var recipientUser = new AppUser
            {
                Id = "u-destinatario",
                Email = recipientEmail,
                FirstName = "Joao",
                LastName = "Recebe",
                UserCategory = catEstudante,
                BirthDate = new DateTime(1995, 5, 5),
                Gender = Projeto_SEGUES.Models.Enums.Gender.Male
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

        [Fact]
        public async Task Index_ReturnsView_WithUserBalance()
        {
            var (user, _, _, controller) = await SetupFullEnv("u-teste", "b@b.pt");

            var result = await controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(15m, controller.ViewBag.UserBalance);
        }

        [Fact]
        public async Task TransferTickets_NoTicketsSelected_ReturnsRedirectWithError()
        {
            // Arrange
            var (_, _, _, controller) = await SetupFullEnv("u-teste", "dest@segues.pt");

            // Act
            var result = await controller.TransferTickets(new List<string>(), "dest@segues.pt");

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("SendTicket", redirect.ActionName);

            // CORREÇÃO: Verificar a chave correta do SweetAlert (SwalData)
            var swalData = controller.TempData["SwalData"]?.ToString();
            Assert.Contains("error", swalData); // Verifica se o ícone é de erro
            Assert.Contains("Por favor, selecione pelo menos uma senha para transferir.", swalData);
        }

        [Fact]
        public async Task TransferTickets_Success_CallsServiceAndRedirects()
        {
            // Arrange
            var currentUserId = "u-transfere";
            var (user, _, mockService, controller) = await SetupFullEnv(currentUserId, "dest@segues.pt");
            var ticketsToTransfer = new List<string> { "TICKET1", "TICKET2" };

            mockService.Setup(s => s.TransferTicketsAsync(currentUserId, "dest@segues.pt", ticketsToTransfer))
                .ReturnsAsync(new ServiceResult(true, "Senhas transferidas com sucesso"));

            // Act
            var result = await controller.TransferTickets(ticketsToTransfer, "dest@segues.pt");

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("SendTicket", redirect.ActionName);

            // CORREÇÃO: Verificar a chave correta do SweetAlert (SwalData)
            var swalData = controller.TempData["SwalData"]?.ToString();
            Assert.Contains("success", swalData); // Verifica se o ícone é de sucesso
            Assert.Contains("Senhas transferidas com sucesso", swalData);

            // Verificar se o serviço foi mesmo chamado
            mockService.Verify(s => s.TransferTicketsAsync(currentUserId, "dest@segues.pt", ticketsToTransfer), Times.Once);
        }

        [Fact]
        public async Task CheckTransferEligibility_SameCategory_ReturnsSuccessJson()
        {
            var email = "amigo@segues.pt";
            var (user, recipient, _, controller) = await SetupFullEnv("u-atual", email);

            var result = await controller.CheckTransferEligibility(email);

            var jsonResult = Assert.IsType<JsonResult>(result);
            var successValue = jsonResult.Value.GetType().GetProperty("success").GetValue(jsonResult.Value, null);
            var recipientName = jsonResult.Value.GetType().GetProperty("recipientName").GetValue(jsonResult.Value, null);

            Assert.Equal(true, successValue);
            Assert.Equal("Joao Recebe", recipientName);
        }
    }
}