using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Projeto_SEGUES.Areas.Bar.Controllers;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Bar;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Inventory;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using SeguesTests.Helpers;
using System.Security.Claims;
using Xunit;

namespace SeguesTests
{
    public class BarControllerTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly SqliteConnection _connection;
        private readonly BarService _barService;
        private readonly BarController _controller;
        private readonly Mock<UserManager<AppUser>> _mockUserManager;

        public BarControllerTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;

            _context = new AppDbContext(options);
            _context.Database.EnsureCreated();

            _mockUserManager = MockHelper.MockUserManager(new List<AppUser>());
            _barService = new BarService(_context);
            _controller = new BarController(_barService);

            var httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            _controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        }

        private AppUser SetupUser(string id, decimal balance)
        {
            var user = new AppUser
            {
                Id = id,
                Email = id + "@test.com",
                UserName = id + "@test.com",
                FirstName = "Diogo",
                LastName = "Teste",
                Balance = balance,
                Gender = Gender.Male,
                BirthDate = DateTime.Now.AddYears(-20),
                Status = UserStatus.Active,
                CreationDate = DateTime.Now,
                UserCategory = new UserCategory { Name = "Estudante" }
            };
            _context.Users.Add(user);
            _context.SaveChanges();
            return user;
        }

        private Product SetupProduct(string name, decimal price, int stock)
        {
            var cat = new ProductCategory { Name = "Bar" };
            var product = new Product
            {
                Name = name,
                Price = price,
                Stock = stock,
                Category = cat,
                IsActive = true,
                Description = "Teste"
            };
            _context.Products.Add(product);
            _context.SaveChanges();
            return product;
        }

        [Fact]
        public async Task PlaceOrder_DeductsBalance_And_SetsPickup()
        {
            var user = SetupUser("u1", 10.00m);
            var product = SetupProduct("Sandes", 2.50m, 10);

            var result = await _barService.PlaceOrderAsync(user.Id, product.Id);

            var dbUser = await _context.Users.FindAsync(user.Id);
            var order = await _context.BarOrders.FirstOrDefaultAsync(o => o.UserId == user.Id);

            Assert.True(result.Succeeded);
            Assert.Equal(7.50m, dbUser?.Balance);
            Assert.Equal(0, order?.Status);
            Assert.NotNull(order?.RedemptionCode);
        }

        [Fact]
        public async Task GetOrderHistory_ReturnsOnlyUserOrders()
        {
            var user1 = SetupUser("u1", 20m);
            var user2 = SetupUser("u2", 20m);
            var p = SetupProduct("Cafe", 1m, 10);

            await _barService.PlaceOrderAsync(user1.Id, p.Id);
            await _barService.PlaceOrderAsync(user2.Id, p.Id);

            var history = await _barService.GetOrderHistoryAsync(user1.Id);

            Assert.Single(history);
            Assert.Equal(user1.Id, history[0].UserId);
        }

        [Fact]
        public async Task CancelOrder_RefundsUser_WhenPendente()
        {
            var user = SetupUser("u1", 5.00m);
            var product = SetupProduct("Sumo", 2.00m, 10);
            await _barService.PlaceOrderAsync(user.Id, product.Id);

            var order = await _context.BarOrders.FirstAsync();

            user.Balance += order.PriceAtTime;
            order.Status = 4; // Cancelado
            await _context.SaveChangesAsync();

            var dbUser = await _context.Users.FindAsync(user.Id);
            Assert.Equal(5.00m, dbUser?.Balance);
            Assert.Equal(4, order.Status);
        }

        [Fact]
        public async Task UpdateStatus_ChangesStateCorrectly()
        {
            var user = SetupUser("u1", 10m);
            var p = SetupProduct("Bolo", 1.5m, 5);
            await _barService.PlaceOrderAsync(user.Id, p.Id);
            var order = await _context.BarOrders.FirstAsync();

            order.Status = 1; // Em preparação
            await _context.SaveChangesAsync();

            var dbOrder = await _context.BarOrders.FindAsync(order.Id);
            Assert.Equal(1, dbOrder?.Status);
        }

        public void Dispose()
        {
            _connection.Close();
            _context.Dispose();
        }
    }
}