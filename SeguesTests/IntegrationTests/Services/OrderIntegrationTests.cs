using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Microsoft.AspNetCore.Identity.UI.Services;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Inventory;
using Projeto_SEGUES.Models.Order;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using Xunit;

namespace SeguesTests.IntegrationTests.Services
{
    public class OrderIntegrationTests
    {
        private AppDbContext GetContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            return new AppDbContext(options);
        }

        private AppUser CreateValidPedro(string id, decimal balance = 100m) => new()
        {
            Id = id,
            FirstName = "Pedro",
            LastName = "Integration",
            Balance = balance,
            BirthDate = DateTime.Now.AddYears(-20),
            Gender = Gender.Male,
            UserCategory = new UserCategory { Name = "Student" }
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
        public async Task AddToCartAsync_PersistsNewOrderLineInDatabase()
        {
            var context = GetContext();
            var service = new OrderService(context, Mock.Of<IAdminService>(), Mock.Of<IEmailSender>(), Mock.Of<ILogger<OrderService>>());

            var user = CreateValidPedro("u-1");
            var product = CreateValidProduct(1, "Pedro-Cafe", 0.80m, 100);
            context.Users.Add(user);
            context.Product.Add(product);
            await context.SaveChangesAsync();

            await service.AddToCartAsync("u-1", 1, 2);

            var cart = await context.Order.Include(o => o.ProductPurchases).FirstOrDefaultAsync(o => o.AppUser.Id == "u-1");
            Assert.NotNull(cart);
            Assert.Single(cart.ProductPurchases);
            Assert.Equal(1.60m, cart.TotalValue);
        }

        [Fact]
        public async Task SubmitOrderAsync_ExecutesFullTransaction_DeductsBalanceAndStock()
        {
            var context = GetContext();
            var adminMock = new Mock<IAdminService>();
            var service = new OrderService(context, adminMock.Object, Mock.Of<IEmailSender>(), Mock.Of<ILogger<OrderService>>());

            var user = CreateValidPedro("u-1", balance: 50m);
            var product = CreateValidProduct(1, "Pedro-Burger", 10m, 20);
            var cart = new Order { Id = 10, AppUser = user, Status = OrderStatus.Cart, TotalValue = 10m };
            cart.ProductPurchases.Add(new OrderLine
            {
                OrderId = 10,
                Order = cart,
                ProductId = 1,
                Product = product,
                Quantity = 1,
                ProductValue = 10m
            });

            context.Users.Add(user);
            context.Product.Add(product);
            context.Order.Add(cart);
            await context.SaveChangesAsync();

            adminMock.Setup(s => s.IsBarOpenAsync(It.IsAny<TimeSpan>())).ReturnsAsync(true);

            await service.SubmitOrderAsync(user, true, null);

            var updatedUser = await context.Users.FindAsync("u-1");
            var updatedProduct = await context.Product.FindAsync(1);
            var transaction = await context.Transaction.FirstOrDefaultAsync(t => t.User.Id == "u-1");

            Assert.Equal(40m, updatedUser!.Balance);
            Assert.Equal(19, updatedProduct!.Stock);
            Assert.NotNull(transaction);
            Assert.Equal(-10m, transaction.Amount);
        }

        [Fact]
        public async Task CancelOrderAsync_RestoresStockAndBalanceCorrectly()
        {
            var context = GetContext();
            var service = new OrderService(context, Mock.Of<IAdminService>(), Mock.Of<IEmailSender>(), Mock.Of<ILogger<OrderService>>());

            var user = CreateValidPedro("u-1", balance: 10m);
            var product = CreateValidProduct(1, "Pedro-Item", 5m, 10);
            var order = new Order { Id = 20, AppUser = user, Status = OrderStatus.Pending, TotalValue = 5m, RedemptionCode = "RECOVERY" };
            order.ProductPurchases.Add(new OrderLine
            {
                OrderId = 20,
                Order = order,
                ProductId = 1,
                Product = product,
                Quantity = 1,
                ProductValue = 5m
            });

            context.Users.Add(user);
            context.Product.Add(product);
            context.Order.Add(order);
            await context.SaveChangesAsync();

            await service.CancelOrderAsync(20);

            var updatedUser = await context.Users.FindAsync("u-1");
            var updatedProduct = await context.Product.FindAsync(1);

            Assert.Equal(15m, updatedUser!.Balance);
            Assert.Equal(11, updatedProduct!.Stock);
            Assert.Equal(OrderStatus.Cancelled, order.Status);
        }

        [Fact]
        public async Task UpdateOrderStatusAsync_PersistsStatusChangeInDatabase()
        {
            var context = GetContext();
            var service = new OrderService(context, Mock.Of<IAdminService>(), Mock.Of<IEmailSender>(), Mock.Of<ILogger<OrderService>>());

            var user = CreateValidPedro("u-1");
            var order = new Order { Id = 30, AppUser = user, Status = OrderStatus.Pending };
            context.Order.Add(order);
            await context.SaveChangesAsync();

            await service.UpdateOrderStatusAsync(30, (int)OrderStatus.Preparing, user);

            var dbOrder = await context.Order.FindAsync(30);
            Assert.Equal(OrderStatus.Preparing, dbOrder!.Status);
        }
    }
}