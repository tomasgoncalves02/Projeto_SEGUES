using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures; 
using Microsoft.EntityFrameworkCore;
using Moq;
using Projeto_SEGUES.Areas.Bar.Controllers;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Bar;
using Projeto_SEGUES.Models.Inventory;
using Projeto_SEGUES.Models.User;
using System.Security.Claims;
using Xunit;

namespace SeguesTests.Bar
{
    public class UserOrdersControllerTests
    {
        private AppDbContext GetDatabaseContext() => new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        private Mock<UserManager<AppUser>> GetMockUserManager() =>
            new Mock<UserManager<AppUser>>(new Mock<IUserStore<AppUser>>().Object, null, null, null, null, null, null, null, null);

        private async Task<(AppUser user, Product product)> SetupFullEnv(AppDbContext context, UserOrdersController controller, string userId)
        {
            var category = new UserCategory { Name = "Cliente" };
            var user = new AppUser
            {
                Id = userId,
                FirstName = "Diogo",
                LastName = "T",
                UserCategory = category,
                BirthDate = new DateTime(1995, 5, 5), 
                Gender = Projeto_SEGUES.Models.Enums.Gender.Other,
                Balance = 100m,
                UserName = "diogo@teste.com"
            };
            var product = new Product { Name = "Cafe", Description = "Expresso", Price = 1m, Stock = 10 };

            context.Users.Add(user);
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }, "TestAuth");
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) } };

            var tempData = new Mock<ITempDataDictionary>();
            controller.TempData = tempData.Object;

            return (user, product);
        }

        [Fact]
        public async Task ConfirmPurchase_Acao_Sucesso()
        {
            using var context = GetDatabaseContext();
            var mockUserMgr = GetMockUserManager();
            var controller = new UserOrdersController(context, mockUserMgr.Object);
            var userId = "u-logado";
            var (user, product) = await SetupFullEnv(context, controller, userId);

            mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

            context.CartItems.Add(new CartItem { UserId = userId, ProductId = product.Id, Quantity = 2 });
            await context.SaveChangesAsync();

            var result = await controller.ConfirmPurchase(true, null);

            Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(98m, user.Balance);
            Assert.Equal(8, product.Stock);
        }

        [Fact]
        public async Task CancelOrder_Acao_Reembolso()
        {
            using var context = GetDatabaseContext();
            var mockUserMgr = GetMockUserManager();
            var controller = new UserOrdersController(context, mockUserMgr.Object);
            var userId = "u-cancel";
            var (user, product) = await SetupFullEnv(context, controller, userId);

            mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

            var order = new BarOrder
            {
                Id = 500,
                RedemptionCode = "REFUND",
                UserId = userId,
                ProductId = product.Id,
                Status = 0,
                PriceAtTime = 10m,
                Quantity = 1,
                CreationTime = DateOnly.FromDateTime(DateTime.Now),
                Expired = DateOnly.FromDateTime(DateTime.Now),
                Product = product
            };
            context.BarOrders.Add(order);
            await context.SaveChangesAsync();

            var result = await controller.CancelOrder("REFUND");

            Assert.Equal(110m, user.Balance); 
            Assert.Equal(4, order.Status); 
            Assert.Equal(11, product.Stock); 
        }

        [Fact]
        public async Task AddToCart_Acao_Sucesso()
        {
            using var context = GetDatabaseContext();
            var mockUserMgr = GetMockUserManager();
            var controller = new UserOrdersController(context, mockUserMgr.Object);
            var userId = "u-cart";
            var (user, product) = await SetupFullEnv(context, controller, userId);

            mockUserMgr.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);

            var result = await controller.AddToCart(product.Id, 1);

            Assert.IsType<JsonResult>(result);
            var cartItem = await context.CartItems.FirstOrDefaultAsync(c => c.UserId == userId);
            Assert.NotNull(cartItem);
            Assert.Equal(1, cartItem.Quantity);
        }
    }
}