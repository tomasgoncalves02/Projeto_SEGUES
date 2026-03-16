using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Inventory;
using Projeto_SEGUES.Models.Order;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using Xunit;

namespace SeguesTests.Services
{
    public class OrderServiceTests
    {
        private readonly Mock<IAdminService> _mockAdminService;
        private readonly Mock<IEmailSender> _mockEmailSender;

        public OrderServiceTests()
        {
            _mockAdminService = new Mock<IAdminService>();
            _mockEmailSender = new Mock<IEmailSender>();
        }

        private AppDbContext GetDatabaseContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            return new AppDbContext(options);
        }

        private AppUser CreatePedroUser(string id, decimal balance = 100m) => new()
        {
            Id = id,
            FirstName = "Pedro",
            LastName = "Order",
            Email = "pedro.order@test.pt",
            Balance = balance,
            BirthDate = DateTime.Now.AddYears(-20),
            Gender = Gender.Male,
            UserCategory = new UserCategory { Name = "Student" }
        };

        // Verifies that a product is added to a new cart and the total value is calculated correctly
        [Fact]
        public async Task AddToCartAsync_NewItem_CalculatesTotalValue()
        {
            var context = GetDatabaseContext();
            var service = new OrderService(context, _mockAdminService.Object, _mockEmailSender.Object);

            var user = CreatePedroUser("u-1");
            var category = new ProductCategory { Id = 10, Name = "Bar", Description = "Bar Products" };
            var product = new Product
            {
                Id = 1,
                Name = "Café",
                Description = "Expresso",
                Price = 0.80m,
                Stock = 100,
                MinimumStock = 5,
                IsActive = true,
                Category = category
            };

            context.Users.Add(user);
            context.ProductCategory.Add(category);
            context.Product.Add(product);
            await context.SaveChangesAsync();

            var result = await service.AddToCartAsync(user.Id, product.Id, 2);

            var cart = await context.Order.Include(o => o.ProductPurchases).FirstAsync();
            Assert.True(result.Success);
            Assert.Equal(1.60m, cart.TotalValue);
            Assert.Single(cart.ProductPurchases);
        }

        // Prevents order submission if the bar is closed for the selected pickup time
        [Fact]
        public async Task SubmitOrderAsync_BarClosed_ReturnsFailure()
        {
            var context = GetDatabaseContext();
            var service = new OrderService(context, _mockAdminService.Object, _mockEmailSender.Object);

            var user = CreatePedroUser("u-1");
            var category = new ProductCategory { Name = "Geral", Description = "General items" };
            var product = new Product { Name = "Sandes", Description = "Mista", Price = 5m, Stock = 10, MinimumStock = 1, Category = category };

            var cart = new Order { Id = 50, AppUser = user, Status = OrderStatus.Cart, TotalValue = 10m };
            cart.ProductPurchases.Add(new OrderLine
            {
                OrderId = 50,
                Order = cart,
                ProductId = 1,
                Product = product,
                Quantity = 2,
                ProductValue = 5m
            });

            context.Users.Add(user);
            context.Order.Add(cart);
            await context.SaveChangesAsync();

            _mockAdminService.Setup(s => s.IsBarOpenAsync(It.IsAny<TimeSpan>())).ReturnsAsync(false);
            _mockAdminService.Setup(s => s.GetOpenBarTimeAsync()).ReturnsAsync(new TimeSpan(8, 0, 0));
            _mockAdminService.Setup(s => s.GetCloseBarTimesAsync()).ReturnsAsync(new TimeSpan(20, 0, 0));

            var result = await service.SubmitOrderAsync(user, true, null);

            Assert.False(result.Success);
            Assert.Contains("Bar encontra-se encerrado", result.Message);
        }

        // Ensures an order is rejected if the user does not have enough balance
        [Fact]
        public async Task SubmitOrderAsync_InsufficientBalance_ReturnsFailure()
        {
            var context = GetDatabaseContext();
            var service = new OrderService(context, _mockAdminService.Object, _mockEmailSender.Object);

            var user = CreatePedroUser("u-1", balance: 1.0m);
            var category = new ProductCategory { Name = "Refeição", Description = "Canteen items" };
            var product = new Product { Name = "Almoço", Description = "Prato do dia", Price = 5.0m, Stock = 10, MinimumStock = 1, Category = category };

            var cart = new Order { Id = 60, AppUser = user, Status = OrderStatus.Cart, TotalValue = 5.0m };
            cart.ProductPurchases.Add(new OrderLine
            {
                OrderId = 60,
                Order = cart,
                ProductId = 2,
                Product = product,
                Quantity = 1,
                ProductValue = 5.0m
            });

            context.Users.Add(user);
            context.Order.Add(cart);
            await context.SaveChangesAsync();

            _mockAdminService.Setup(s => s.IsBarOpenAsync(It.IsAny<TimeSpan>())).ReturnsAsync(true);

            var result = await service.SubmitOrderAsync(user, true, null);

            Assert.False(result.Success);
            Assert.Equal("Saldo insuficiente.", result.Message);
        }

        // Confirms that a successful order deducts balance, updates stock, and generates a redemption code
        [Fact]
        public async Task SubmitOrderAsync_Success_UpdatesStockAndBalance()
        {
            var context = GetDatabaseContext();
            var service = new OrderService(context, _mockAdminService.Object, _mockEmailSender.Object);

            var user = CreatePedroUser("u-1", balance: 20m);
            var category = new ProductCategory { Name = "Bar", Description = "Snacks" };
            var product = new Product { Name = "Sandes", Description = "Atum", Price = 2.5m, Stock = 10, MinimumStock = 1, Category = category };

            var cart = new Order { Id = 70, AppUser = user, Status = OrderStatus.Cart, TotalValue = 2.5m, RedemptionCode = "TEMP123" };
            cart.ProductPurchases.Add(new OrderLine
            {
                OrderId = 70,
                Order = cart,
                ProductId = 3,
                Product = product,
                Quantity = 1,
                ProductValue = 2.5m
            });

            context.Users.Add(user);
            context.Order.Add(cart);
            await context.SaveChangesAsync();

            _mockAdminService.Setup(s => s.IsBarOpenAsync(It.IsAny<TimeSpan>())).ReturnsAsync(true);

            var result = await service.SubmitOrderAsync(user, true, null);

            Assert.True(result.Success);
            Assert.Equal(17.5m, user.Balance);
            Assert.Equal(9, product.Stock);
            Assert.Equal(OrderStatus.Pending, cart.Status);
        }

        // Verifies that cancelling a pending order restores stock and refunds the user's balance
        [Fact]
        public async Task CancelOrderAsync_PendingOrder_RefundsUser()
        {
            var context = GetDatabaseContext();
            var service = new OrderService(context, _mockAdminService.Object, _mockEmailSender.Object);

            var user = CreatePedroUser("u-1", balance: 10m);
            var category = new ProductCategory { Name = "Misc", Description = "Miscellaneous" };
            var product = new Product { Id = 4, Name = "Item", Description = "Test Item", Stock = 5, MinimumStock = 1, Price = 5m, Category = category };

            var order = new Order { Id = 80, AppUser = user, Status = OrderStatus.Pending, TotalValue = 5m, RedemptionCode = "CANCEL1" };
            order.ProductPurchases.Add(new OrderLine
            {
                OrderId = 80,
                Order = order,
                ProductId = 4,
                Product = product,
                Quantity = 1,
                ProductValue = 5m
            });

            context.Users.Add(user);
            context.Order.Add(order);
            await context.SaveChangesAsync();

            var result = await service.CancelOrderAsync(order.Id);

            Assert.True(result.Success);
            Assert.Equal(15m, user.Balance);
            Assert.Equal(6, product.Stock);
            Assert.Equal(OrderStatus.Cancelled, order.Status);
        }
    }
}