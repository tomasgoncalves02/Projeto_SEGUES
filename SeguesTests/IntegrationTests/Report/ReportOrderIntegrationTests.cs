using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Admin;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Order;
using Projeto_SEGUES.Models.User;
using SeguesTests.Helpers;
using System;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Projeto_SEGUES;
using Xunit;

namespace SeguesTests.IntegrationTests.Report
{
    public class ReportOrderIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly SqliteConnection _connection;

        public ReportOrderIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    var descriptors = services.Where(d =>
                        d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                        d.ServiceType == typeof(AppDbContext)).ToList();

                    foreach (var d in descriptors) services.Remove(d);

                    services.AddDbContext<AppDbContext>(options =>
                    {
                        options.UseSqlite(_connection);
                    });

                    services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = "Test";
                        options.DefaultChallengeScheme = "Test";
                        options.DefaultScheme = "Test";
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", null);

                    services.AddAuthorization();
                });
            });

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        }

        private async Task SeedPedroOrders()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (db.Users.Any(u => u.Id == "pedro-77")) return;

            var category = new UserCategory { Name = "Estudante" };
            db.UserCategory.Add(category);

            var pedro = new AppUser
            {
                Id = "pedro-77",
                UserName = "pedro@segues.pt",
                FirstName = "Pedro",
                LastName = "Jesus",
                Email = "pedro@segues.pt",
                BirthDate = new DateTime(2000, 1, 1),
                Balance = 100.00m,
                Gender = Gender.Male,
                UserCategory = category,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            var order = new Order
            {
                Id = 10,
                AppUser = pedro,
                OrderDate = DateTime.Now,
                Status = OrderStatus.Pending,
                RedemptionCode = "ORD-PEDRO-1"
            };

            db.Users.Add(pedro);
            db.Order.Add(order);
            await db.SaveChangesAsync();
        }

        [Fact]
        public async Task Index_DisplaysPedroOrderHistory()
        {
            await SeedPedroOrders();
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", "User");

            var response = await client.GetAsync("/Report/ReportOrder/Index");
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("ORD-PEDRO-1", content);
        }

        [Fact]
        public async Task GetFilteredOrders_ReturnsPartialView_WithOrderData()
        {
            await SeedPedroOrders();
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", "User");

            var response = await client.GetAsync("/Report/ReportOrder/GetFilteredOrders");
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("ORD-PEDRO-1", content);
        }

        public void Dispose()
        {
            _connection?.Close();
            _connection?.Dispose();
        }
    }
}