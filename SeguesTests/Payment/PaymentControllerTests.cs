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





        
    }
}