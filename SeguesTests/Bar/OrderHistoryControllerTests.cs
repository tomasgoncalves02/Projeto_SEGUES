using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Projeto_SEGUES.Areas.Bar.Controllers;
using Projeto_SEGUES.Areas.Bar.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Bar; // Ajusta para o teu namespace do model BarOrder
using System.Security.Claims;
using Xunit;

namespace SeguesTests.Bar
{
    public class OrderHistoryControllerTests
    {
        private AppDbContext GetDatabaseContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var databaseContext = new AppDbContext(options);
            databaseContext.Database.EnsureCreated();
            return databaseContext;
        }

        private void MockUser(OrderHistoryController controller, string userId)
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        [Fact]
        public async Task Index_NoUser_RedirectsToLogin()
        {
            using var context = GetDatabaseContext();
            var controller = new OrderHistoryController(context);

            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity());
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            var result = await controller.Index();

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
            Assert.Equal("Account", redirectResult.ControllerName);
        }

        [Fact]
        public async Task Index_ReturnsViewWithMappedOrders()
        {
            using var context = GetDatabaseContext();
            var userId = "user-123";

            context.BarOrders.AddRange(new List<BarOrder>
            {
                new BarOrder
                {
                    Id = 1,
                    UserId = userId,
                    OrderDate = DateTime.Now.AddDays(-1),
                    Status = 0, // Pendente
                    RedemptionCode = "AAA111",
                    PriceAtTime = 5.00m
                },
                new BarOrder
                {
                    Id = 2,
                    UserId = userId,
                    OrderDate = DateTime.Now,
                    Status = 3, // Entregue
                    RedemptionCode = "BBB222",
                    PriceAtTime = 2.50m
                }
            });
            await context.SaveChangesAsync();

            var controller = new OrderHistoryController(context);
            MockUser(controller, userId);

            var result = await controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<List<OrderHistoryViewModel>>(viewResult.Model);

            Assert.Equal(2, model.Count);
            Assert.Equal("Entregue", model[0].Estado);
            Assert.Equal("Pendente", model[1].Estado);
            Assert.Equal("BBB222", model[0].Codigo);
        }

        [Fact]
        public async Task Index_UserWithNoOrders_ReturnsEmptyList()
        {
            using var context = GetDatabaseContext();
            var controller = new OrderHistoryController(context);
            MockUser(controller, "user-sem-pedidos");

            var result = await controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<List<OrderHistoryViewModel>>(viewResult.Model);
            Assert.Empty(model);
        }
    }
}