using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Projeto_SEGUES.Areas.Ticket;
using Projeto_SEGUES.Areas.Ticket.ViewModels;
using Projeto_SEGUES.Models.Ticket;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using System.Security.Claims;
using Xunit;

namespace SeguesTests.Tickets
{
    public class TicketValidationControllerTests
    {
        private Mock<UserManager<AppUser>> GetMockUserManager() =>
            new Mock<UserManager<AppUser>>(new Mock<IUserStore<AppUser>>().Object, null, null, null, null, null, null, null, null);

        private (TicketValidationController, Mock<ITicketService>, Mock<UserManager<AppUser>>, AppUser) SetupController()
        {
            var mockService = new Mock<ITicketService>();
            var mockUserMgr = GetMockUserManager();

            var controller = new TicketValidationController(mockUserMgr.Object, mockService.Object);

            var user = new AppUser
            {
                Id = "u-admin",
                FirstName = "Admin",
                LastName = "Test",
                UserCategory = new UserCategory { Name = "Staff" },
                BirthDate = new DateTime(1990, 1, 1),
                Gender = Projeto_SEGUES.Models.Enums.Gender.Other
            };

            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, user.Id) }, "TestAuth");
            var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };

            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

            mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

            return (controller, mockService, mockUserMgr, user);
        }

        [Fact]
        public async Task Index_Get_ReturnsViewWithRecentTickets()
        {
            var (controller, mockService, _, _) = SetupController();
            var tickets = new List<Projeto_SEGUES.Models.Ticket.Ticket>();
            mockService.Setup(s => s.GetRecentUsedTicketsAsync(It.IsAny<int>())).ReturnsAsync(tickets);
            var result = await controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ValidateTicketViewModel>(viewResult.Model);
            Assert.NotNull(model.RecentTickets);
        }

        [Fact]
        public async Task Index_Post_InvalidModel_ReturnsView()
        {
            var (controller, mockService, _, _) = SetupController();
            controller.ModelState.AddModelError("Code", "Required");
            var model = new ValidateTicketViewModel();

            var result = await controller.Index(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            mockService.Verify(s => s.GetRecentUsedTicketsAsync(It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public async Task Index_Post_ValidModel_Success_SetsTempData()
        {
            var (controller, mockService, _, user) = SetupController();
            var model = new ValidateTicketViewModel { Code = "VALID123" };

            mockService.Setup(s => s.ValidateTicketAsync("VALID123", user))
                .ReturnsAsync(new ServiceResult(true, "Senha validada com sucesso."));
            mockService.Setup(s => s.GetRecentUsedTicketsAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<Projeto_SEGUES.Models.Ticket.Ticket>());

            var result = await controller.Index(model);

            var viewResult = Assert.IsType<ViewResult>(result);

            Assert.NotEmpty(controller.TempData);
            var tempDataValue = controller.TempData.Values.FirstOrDefault()?.ToString();
            Assert.NotNull(tempDataValue);
            Assert.Contains("Senha validada com sucesso.", tempDataValue);

            Assert.True(controller.ModelState.IsValid);
            Assert.Empty(model.Code);
        }

        [Fact]
        public async Task Index_Post_ValidModel_Error_SetsTempData()
        {
            var (controller, mockService, _, user) = SetupController();
            var model = new ValidateTicketViewModel { Code = "INVALID" };

            mockService.Setup(s => s.ValidateTicketAsync("INVALID", user))
                .ReturnsAsync(new ServiceResult(false, "Senha invalida."));
            mockService.Setup(s => s.GetRecentUsedTicketsAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<Projeto_SEGUES.Models.Ticket.Ticket>());

            var result = await controller.Index(model);

            var viewResult = Assert.IsType<ViewResult>(result);

            Assert.NotEmpty(controller.TempData);
            var tempDataValue = controller.TempData.Values.FirstOrDefault()?.ToString();
            Assert.NotNull(tempDataValue);
            Assert.Contains("Senha invalida.", tempDataValue);
        }
    }
}