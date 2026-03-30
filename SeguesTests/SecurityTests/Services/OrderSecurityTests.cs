using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Areas;
using Projeto_SEGUES.Models.Inventory;
using Projeto_SEGUES.Models.Order;
using Projeto_SEGUES.Models.User;
using Microsoft.AspNetCore.Identity.UI.Services;
using Projeto_SEGUES.Services;
using Xunit;

namespace SeguesTests.SecurityTests.Services
{
    public class OrderSecurityTests
    {
        private AppDbContext GetContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private AppUser CreateValidPedro(string id, decimal balance = 100m) => new()
        {
            Id = id,
            FirstName = "Pedro",
            LastName = "Security",
            Balance = balance,
            BirthDate = DateTime.Now.AddYears(-20),
            Gender = Gender.Male,
            UserCategory = new UserCategory { Name = "Pedro-Student" }
        };

        private Product CreateValidProduct(int id, string name, decimal price, int stock) => new()
        {
            Id = id,
            Name = name,
            Price = price,
            Stock = stock,
            MinimumStock = 1,
            Description = "D",
            Category = new ProductCategory { Name = "Bar", Description = "D" }
        };

        [Fact]
        public async Task SubmitOrderAsync_InsufficientBalance_ReturnsFailure()
        {
            var context = GetContext();
            var adminMock = new Mock<IAdminService>();
            var service = new OrderService(context, adminMock.Object, Mock.Of<IEmailSender>(), Mock.Of<ILogger<OrderService>>());

            var user = CreateValidPedro("u-1", balance: 5m);
            var product = CreateValidProduct(1, "Pedro-Sandes", 10m, 50);
            var cart = new Order { Id = 1, AppUser = user, Status = OrderStatus.Cart, TotalValue = 10m };

            cart.ProductPurchases.Add(new OrderLine
            {
                OrderId = 1,
                Order = cart,
                ProductId = 1,
                Product = product,
                Quantity = 1,
                ProductValue = 10m
            });

            context.Users.Add(user);
            context.Order.Add(cart);
            await context.SaveChangesAsync();
            adminMock.Setup(s => s.IsBarOpenAsync(It.IsAny<TimeSpan>())).ReturnsAsync(true);

            var result = await service.SubmitOrderAsync(user, true, null);

            Assert.False(result.Success);
            Assert.Equal("Saldo insuficiente.", result.Message);
        }

        [Fact]
        public async Task SubmitOrderAsync_BarClosed_ReturnsFailure()
        {
            var context = GetContext();
            var adminMock = new Mock<IAdminService>();
            var service = new OrderService(context, adminMock.Object, Mock.Of<IEmailSender>(), Mock.Of<ILogger<OrderService>>());

            var user = CreateValidPedro("u-1");
            var cart = new Order { Id = 1, AppUser = user, Status = OrderStatus.Cart };
            cart.ProductPurchases.Add(new OrderLine
            {
                OrderId = 1,
                Order = cart,
                ProductId = 1,
                Product = CreateValidProduct(1, "P", 1, 1),
                Quantity = 1,
                ProductValue = 1m
            });

            context.Users.Add(user);
            context.Order.Add(cart);
            await context.SaveChangesAsync();

            adminMock.Setup(s => s.IsBarOpenAsync(It.IsAny<TimeSpan>())).ReturnsAsync(false);
            adminMock.Setup(s => s.GetScheduleAsync()).ReturnsAsync(new Projeto_SEGUES.Areas.Admin.ViewModels.BarCanteenConfigViewModel());

            var result = await service.SubmitOrderAsync(user, true, null);

            Assert.False(result.Success);
            Assert.Contains("Bar encontra-se encerrado", result.Message);
        }

        [Fact]
        public async Task SubmitOrderAsync_OutOfStock_ReturnsFailure()
        {
            var context = GetContext();
            var adminMock = new Mock<IAdminService>();
            var service = new OrderService(context, adminMock.Object, Mock.Of<IEmailSender>(), Mock.Of<ILogger<OrderService>>());

            var user = CreateValidPedro("u-1");
            var product = CreateValidProduct(1, "Pedro-No-Stock", 10m, 0);
            var cart = new Order { Id = 1, AppUser = user, Status = OrderStatus.Cart, TotalValue = 10m };

            cart.ProductPurchases.Add(new OrderLine
            {
                OrderId = 1,
                Order = cart,
                ProductId = 1,
                Product = product,
                Quantity = 1,
                ProductValue = 10m
            });

            context.Users.Add(user);
            context.Order.Add(cart);
            await context.SaveChangesAsync();
            adminMock.Setup(s => s.IsBarOpenAsync(It.IsAny<TimeSpan>())).ReturnsAsync(true);

            var result = await service.SubmitOrderAsync(user, true, null);

            Assert.False(result.Success);
            Assert.Contains("Stock insuficiente", result.Message);
        }

        [Fact]
        public async Task UpdateOrderStatusAsync_InvalidTransition_ReturnsFailure()
        {
            var context = GetContext();
            var service = new OrderService(context, Mock.Of<IAdminService>(), Mock.Of<IEmailSender>(), Mock.Of<ILogger<OrderService>>());

            var user = CreateValidPedro("u-1");
            var order = new Order { Id = 50, AppUser = user, Status = OrderStatus.Pending };
            context.Order.Add(order);
            await context.SaveChangesAsync();

            var result = await service.UpdateOrderStatusAsync(50, (int)OrderStatus.Delivered, user);

            Assert.False(result.Success);
            Assert.Equal("Transição de status inválida.", result.Message);
        }

        [Fact]
        public async Task ValidateOrderCodeAsync_IncorrectCode_ReturnsFailure()
        {
            var context = GetContext();
            var service = new OrderService(context, Mock.Of<IAdminService>(), Mock.Of<IEmailSender>(), Mock.Of<ILogger<OrderService>>());

            var order = new Order { Id = 10, RedemptionCode = "PEDRO-123", AppUser = CreateValidPedro("u-1") };
            context.Order.Add(order);
            await context.SaveChangesAsync();

            var staffMember = CreateValidPedro("staff-1");

            var result = await service.ValidateOrderCodeAsync(10, "CODIGO-ERRADO", staffMember);

            Assert.False(result.Success);
            Assert.Equal("Código inválido!", result.Message);
        }
    }
}