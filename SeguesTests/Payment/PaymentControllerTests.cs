using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.Protected;
using Projeto_SEGUES.Areas.Payment;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Payment;
using Projeto_SEGUES.Models.User;
using System.Security.Claims;
using System.Net;
using Xunit;

namespace SeguesTests.Payment
{
    public class PaymentControllerTests
    {
        private AppDbContext GetDatabaseContext() => new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        private Mock<UserManager<AppUser>> GetMockUserManager() =>
            new Mock<UserManager<AppUser>>(new Mock<IUserStore<AppUser>>().Object, null, null, null, null, null, null, null, null);

        private async Task<(AppUser user, Mock<IHttpClientFactory> mockFactory)> SetupEnv(AppDbContext context, PaymentController controller, string userId)
        {
            var category = new UserCategory { Name = "E" };
            var user = new AppUser
            {
                Id = userId,
                FirstName = "D",
                LastName = "T",
                UserCategory = category,
                BirthDate = new DateTime(1990, 1, 1),
                Gender = Projeto_SEGUES.Models.Enums.Gender.Other,
                Balance = 10m
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }, "TestAuth");
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) } };

            controller.TempData = new Mock<ITempDataDictionary>().Object;

            var mockFactory = new Mock<IHttpClientFactory>();
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });

            var client = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://test.com") };
            mockFactory.Setup(f => f.CreateClient("MbWayClient")).Returns(client);

            return (user, mockFactory);
        }

        [Fact]
        public async Task InitiateMbWay_CriaTransacaoERetornaWaiting()
        {
            using var context = GetDatabaseContext();
            var mockUserMgr = GetMockUserManager();
            var userId = "u-mbway";

            var mockFactoryBase = new Mock<IHttpClientFactory>();
            var controller = new PaymentController(context, mockFactoryBase.Object, mockUserMgr.Object);

            var (user, mockFactory) = await SetupEnv(context, controller, userId);
            var finalController = new PaymentController(context, mockFactory.Object, mockUserMgr.Object)
            {
                ControllerContext = controller.ControllerContext,
                TempData = controller.TempData
            };

            mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

            var result = await finalController.InitiateMbWay(20.00m, "912345678");

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Waiting", viewResult.ViewName);
            var transaction = await context.Set<Transaction>().FirstOrDefaultAsync(t => t.PhoneNumber == "912345678");
            Assert.NotNull(transaction);
            Assert.Equal(20.00m, transaction.Amount);
        }

        [Fact]
        public async Task Callback_Sucesso_AtualizaSaldoEStatus()
        {
            using var context = GetDatabaseContext();
            var userId = "u-callback";
            var controller = new PaymentController(context, new Mock<IHttpClientFactory>().Object, GetMockUserManager().Object);
            var (user, _) = await SetupEnv(context, controller, userId);

            var trans = new Transaction
            {
                Reference = "REF123",
                Amount = 50.00m,
                User = user,
                PhoneNumber = "91",
                IsPaid = false
            };
            context.Set<Transaction>().Add(trans);
            await context.SaveChangesAsync();

            var result = await controller.Callback("REF123", "success");

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("Home", redirect.ControllerName);

            Assert.Equal(60.00m, user.Balance); 
            Assert.True(trans.IsPaid);
        }

        [Fact]
        public async Task Callback_Falha_NaoAlteraSaldo()
        {
            using var context = GetDatabaseContext();
            var userId = "u-fail";
            var controller = new PaymentController(context, new Mock<IHttpClientFactory>().Object, GetMockUserManager().Object);
            var (user, _) = await SetupEnv(context, controller, userId);

            var trans = new Transaction { Reference = "REF_FAIL", Amount = 50.00m, User = user, PhoneNumber = "912345678", IsPaid = false };
            context.Set<Transaction>().Add(trans);
            await context.SaveChangesAsync();

            await controller.Callback("REF_FAIL", "fail");

            Assert.Equal(10.00m, user.Balance); 
            Assert.False(trans.IsPaid);
        }
    }
}