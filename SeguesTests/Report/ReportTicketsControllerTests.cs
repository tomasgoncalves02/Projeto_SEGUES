using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Projeto_SEGUES.Areas.Report;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using System.Security.Claims;
using Xunit;

namespace SeguesTests.Report
{
    public class ReportTicketsControllerTests
    {
        private Mock<UserManager<AppUser>> GetMockUserManager() =>
            new Mock<UserManager<AppUser>>(new Mock<IUserStore<AppUser>>().Object, null, null, null, null, null, null, null, null);

        private AppUser CreateValidUser(string id) => new AppUser
        {
            Id = id,
            FirstName = "Diogo",
            LastName = "Report",
            UserCategory = new UserCategory { Name = "Estudante" }, 
            BirthDate = new DateTime(2000, 1, 1),                
            Gender = Projeto_SEGUES.Models.Enums.Gender.Other    
        };

        [Fact]
        public async Task Index_ReturnsViewWithFilteredTickets()
        {
            var mockService = new Mock<ITicketService>();
            var mockUserMgr = GetMockUserManager();
            var controller = new ReportTicketsController(mockService.Object, mockUserMgr.Object);

            var userId = "u-report";
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }, "Test");
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) } };

            mockUserMgr.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);

            mockService.Setup(s => s.QueryHistoryAsync(userId, It.IsAny<string>(), It.IsAny<TicketState?>(), It.IsAny<string>(), It.IsAny<DateTime?>()))
                .ReturnsAsync(new List<Projeto_SEGUES.Models.Ticket.Ticket>());

            var result = await controller.Index(null, null, null, null);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult.Model);
        }

        [Fact]
        public async Task GetFilteredHistory_ReturnsPartialView()
        {
            var mockService = new Mock<ITicketService>();
            var mockUserMgr = GetMockUserManager();
            var controller = new ReportTicketsController(mockService.Object, mockUserMgr.Object);

            var userId = "u-partial";
            mockUserMgr.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);

            mockService.Setup(s => s.QueryHistoryAsync(userId, It.IsAny<string>(), It.IsAny<TicketState?>(), It.IsAny<string>(), It.IsAny<DateTime?>()))
                .ReturnsAsync(new List<Projeto_SEGUES.Models.Ticket.Ticket>());

            var result = await controller.GetFilteredHistory("0", null, "all", "");

            var partialResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_TicketHistoryRows", partialResult.ViewName);
        }
    }
}