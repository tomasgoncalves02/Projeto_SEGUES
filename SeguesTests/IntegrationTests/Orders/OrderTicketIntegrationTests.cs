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

namespace SeguesTests.IntegrationTests.Orders
{
    public class OrderTicketIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly AppDbContext _sharedDb;
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<AppDbContext> _dbOptions;

        public OrderTicketIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;

            _sharedDb = new AppDbContext(_dbOptions);
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

                    services.AddSingleton(_dbOptions);
                    services.AddSingleton(_sharedDb);

                    services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = "Test";
                        options.DefaultChallengeScheme = "Test";
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", null);
                });
            });
        }

        private async Task SeedPedroData()
        {
            var category = new UserCategory { Name = "Estudante" };
            _sharedDb.UserCategory.Add(category);

            _sharedDb.AppConfig.Add(new AppConfig { TicketValidityDays = 30 });

            var pedro = new AppUser
            {
                Id = "pedro-77",
                UserName = "Pedro",
                NormalizedUserName = "PEDRO",       
                FirstName = "Pedro",
                LastName = "Jesus",
                Email = "pedro@segues.pt",
                NormalizedEmail = "PEDRO@SEGUES.PT", 
                BirthDate = new DateTime(2000, 1, 1),
                Balance = 85.50m,
                Gender = Gender.Male,
                UserCategory = category,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            _sharedDb.TicketPrice.Add(new TicketPrice
            {
                Price = 2.50m,
                UserCategory = category,
                InitialDatePrice = DateTime.Now.AddDays(-1)
            });

            _sharedDb.Users.Add(pedro);
            await _sharedDb.SaveChangesAsync();
        }

        [Fact]
        public async Task Index_ReturnsSuccessAndDisplaysCorrectPedroData()
        {
            await SeedPedroData();
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", "User");

            var response = await client.GetAsync("/Order/OrderTicket/Index");
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("85,50", content);  
            Assert.Contains("2,50", content);   
            Assert.Contains("Comprar Senhas", content); 
        }

        public void Dispose()
        {
            _sharedDb?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
        }
    }
}