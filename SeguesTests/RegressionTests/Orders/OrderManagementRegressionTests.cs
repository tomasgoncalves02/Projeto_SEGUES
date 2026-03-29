using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Inventory;
using Projeto_SEGUES.Models.Order;
using Projeto_SEGUES.Models.User;
using SeguesTests.Helpers;
using System;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Xunit;

namespace SeguesTests.RegressionTests.Orders
{
    public class OrderManagementRegressionTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly AppDbContext _sharedDb;
        private readonly SqliteConnection _connection;

        public OrderManagementRegressionTests(WebApplicationFactory<Program> factory)
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;

            _sharedDb = new AppDbContext(dbOptions);
            _sharedDb.Database.EnsureCreated();

            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    var descriptors = services.Where(d =>
                        d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                        d.ServiceType == typeof(AppDbContext) ||
                        d.ServiceType == typeof(DbContextOptions)).ToList();
                    foreach (var d in descriptors) services.Remove(d);

                    services.AddSingleton(dbOptions);
                    services.AddSingleton(_sharedDb);
                    services.AddSingleton<DbContextOptions<AppDbContext>>(dbOptions);

                    services.AddSingleton<IAntiforgery, NoOpAntiforgery>();

                    services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = "Test";
                        options.DefaultChallengeScheme = "Test";
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", null);
                });
            });
        }

        [Fact]
        public async Task CancelOrder_ShouldRefundUserBalance_AndRestoreProductStock()
        {
            decimal initialBalance = 50m;
            int currentStock = 10;
            decimal orderValue = 10m;

            var category = new UserCategory { Name = "Vip" };
            _sharedDb.UserCategory.Add(category);

            var pedro = new AppUser
            {
                Id = "pedro-77",
                UserName = "Pedro",
                FirstName = "Pedro",
                LastName = "Jesus",
                Email = "pedro@segues.pt",
                BirthDate = new DateTime(2000, 1, 1),
                Balance = initialBalance,
                Gender = Gender.Male,
                UserCategory = category,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            var product = new Product
            {
                Id = 1,
                Name = "Cerveja de Teste",
                Price = 10m,
                Stock = currentStock,
                MinimumStock = 5,
                Description = "Teste",
                Category = new ProductCategory { Name = "Bebidas", Description = "Bebidas" }
            };

            var order = new Order
            {
                Id = 1,
                AppUser = pedro,
                Status = OrderStatus.Pending,
                TotalValue = 10m,
                RedemptionCode = "REFUND77",
                OrderDate = DateTime.Now
            };

            order.ProductPurchases.Add(new OrderLine
            {
                OrderId = 1,
                Order = order,
                ProductId = 1,
                Product = product,
                Quantity = 1,
                ProductValue = 10m
            });

            _sharedDb.Users.Add(pedro);
            _sharedDb.Product.Add(product);
            _sharedDb.Order.Add(order);
            await _sharedDb.SaveChangesAsync();

            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", "User");

            var response = await client.PostAsync("/Order/ActiveOrder/CancelOrder/1", null);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

            _sharedDb.ChangeTracker.Clear();

            var pedroUser = await _sharedDb.Users.FirstAsync(u => u.Id == "pedro-77");
            var verifyProduct = await _sharedDb.Product.FirstAsync(p => p.Id == 1);
            var verifyOrder = await _sharedDb.Order.FirstAsync(o => o.Id == 1);
            var transaction = await _sharedDb.Transaction
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.User.Id == "pedro-77");

            Assert.Equal(initialBalance + orderValue, pedroUser.Balance);
            Assert.Equal(currentStock + 1, verifyProduct.Stock);
            Assert.Equal(OrderStatus.Cancelled, verifyOrder.Status);
            Assert.NotNull(transaction);
            Assert.Equal(orderValue, transaction.Amount);
        }
    }
}