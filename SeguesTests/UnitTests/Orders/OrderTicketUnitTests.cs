using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Identity;
using Moq;
using Projeto_SEGUES.Areas.Order;
using Projeto_SEGUES.Models.Ticket;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Services;
using System.Security.Claims;
using Projeto_SEGUES.Areas.Order.Controllers;
using Xunit;

namespace SeguesTests.UnitTests.Orders
{
    public class OrderTicketUnitTests
    {
        private readonly Mock<ITicketService> _mockTicketService;
        private readonly Mock<UserManager<AppUser>> _mockUserManager;
        private readonly OrderTicketController _controller;

        public OrderTicketUnitTests()
        {
            _mockTicketService = new Mock<ITicketService>();
            var store = new Mock<IUserStore<AppUser>>();
            _mockUserManager = new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);

            _controller = new OrderTicketController(_mockTicketService.Object, _mockUserManager.Object);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "pedro-77")
            }, "Test"));

            var httpContext = new DefaultHttpContext { User = user };
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            _controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        }

        [Fact]
        public async Task Index_ValidPedro_ReturnsViewWithBagData()
        {
            var pedro = new AppUser
            {
                Id = "pedro-77",
                Balance = 15.00m,
                FirstName = "Pedro",
                LastName = "Jesus",
                BirthDate = new DateTime(2000, 1, 1),
                Gender = Gender.Male,
                UserCategory = new UserCategory { Name = "Estudante" }
            };

            _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(pedro);
            _mockTicketService.Setup(s => s.GetCurrentPriceForUserAsync(pedro)).ReturnsAsync(2.50m);
            _mockTicketService.Setup(s => s.GetUserTicketsAsync(pedro.Id)).ReturnsAsync(new List<Ticket>());

            var result = await _controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(15.00m, _controller.ViewBag.UserBalance);
            Assert.Equal(2.50m, _controller.ViewBag.CurrentPrice);
        }

        [Fact]
        public async Task BuyTicket_Success_RedirectsToActiveTickets()
        {
            _mockTicketService.Setup(s => s.BuyTicketsAsync("pedro-77", 1))
                .ReturnsAsync(ServiceResult.Ok("Sucesso"));

            var result = await _controller.BuyTicket(1);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("ActiveTickets", redirect.ActionName);
            Assert.Equal("Ticket", redirect.ControllerName);
        }
    }
}