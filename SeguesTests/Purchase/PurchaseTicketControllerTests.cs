using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Projeto_SEGUES.Areas.Purchase;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using System.Security.Claims;
using Xunit;

namespace SeguesTests.Purchase
{
    public class PurchaseTicketControllerTests
    {
        private Mock<UserManager<AppUser>> GetMockUserManager() =>
            new Mock<UserManager<AppUser>>(new Mock<IUserStore<AppUser>>().Object, null, null, null, null, null, null, null, null);

        [Fact]
        public async Task Index_ReturnsView_WithPriceAndBalance()
        {
            var mockService = new Mock<ITicketService>();
            var mockUserMgr = GetMockUserManager();
            var controller = new PurchaseTicketController(mockService.Object, mockUserMgr.Object);

            var userId = "u-ticket";
            var user = new AppUser { Id = userId, Balance = 20m, FirstName = "D", LastName = "T" ,UserCategory = new UserCategory { Name = "Estudante" }, BirthDate = new DateTime(2000, 1, 1), Gender = Projeto_SEGUES.Models.Enums.Gender.Other }; 

            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }, "Test");
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) } };

            mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
            mockService.Setup(s => s.GetCurrentPriceForUserAsync(user)).ReturnsAsync(2.5m);

            
            var result = await controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(20m, controller.ViewBag.UserBalance);
            Assert.Equal(2.5m, controller.ViewBag.CurrentPrice);
        }

        [Fact]
        public async Task BuyTicket_Success_SetsTempData()
        {
            var mockService = new Mock<ITicketService>();
            var mockUserMgr = GetMockUserManager();
            var controller = new PurchaseTicketController(mockService.Object, mockUserMgr.Object);

            controller.TempData = new Mock<ITempDataDictionary>().Object;

            var userId = "u-ticket";
            mockUserMgr.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);

            mockService.Setup(s => s.BuyTicketsAsync(userId, 1))
     .ReturnsAsync(new ServiceResult(true, "Ticket comprado!"));

            var result = await controller.BuyTicket(1);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
        }
    }
}