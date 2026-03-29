using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Admin;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Ticket;
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
    public class OrderTicketRegressionTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly AppDbContext _sharedDb;
        private readonly SqliteConnection _connection;

        public OrderTicketRegressionTests(WebApplicationFactory<Program> factory)
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

                    services.AddSingleton(_sharedDb);
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

        private async Task SeedPedro(decimal balance)
        {
            var category = new UserCategory { Name = "Estudante" };
            _sharedDb.UserCategory.Add(category);

            var config = new AppConfig { TicketValidityDays = 30 };
            _sharedDb.AppConfig.Add(config);

            var pedro = new AppUser
            {
                Id = "pedro-77",       
                UserName = "Pedro",
                FirstName = "Pedro",
                LastName = "Jesus",
                Email = "pedro@segues.pt",
                BirthDate = new DateTime(2000, 1, 1),
                Balance = balance,
                Gender = Gender.Male,
                UserCategory = category,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            var ticketPrice = new TicketPrice
            {
                Price = 2.50m,
                UserCategory = category,
                InitialDatePrice = DateTime.Now.AddDays(-1)
            };

            _sharedDb.Users.Add(pedro);
            _sharedDb.TicketPrice.Add(ticketPrice);
            await _sharedDb.SaveChangesAsync();
        }

        [Fact]
        public async Task BuyTicket_Success_DeductsCorrectAmountFromBalance()
        {
            decimal initialBalance = 10.00m;
            decimal ticketPriceValue = 2.50m;
            await SeedPedro(initialBalance);

            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", "User");

            var response = await client.PostAsync("/Order/OrderTicket/BuyTicket?quantity=2", null);

            var location = response.Headers.Location?.ToString();
            Assert.Contains("ActiveTickets", location);

            _sharedDb.ChangeTracker.Clear();
            var pedroAfter = await _sharedDb.Users.FirstAsync(u => u.Id == "pedro-77"); 

            decimal expectedBalance = initialBalance - (ticketPriceValue * 2);
            Assert.Equal(expectedBalance, pedroAfter.Balance);
        }

        public void Dispose()
        {
            _sharedDb.Dispose();
            _connection.Close();
        }
    }
}