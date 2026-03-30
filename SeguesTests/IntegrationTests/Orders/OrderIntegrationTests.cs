using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Admin;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using SeguesTests.Helpers;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Projeto_SEGUES;
using Xunit;

namespace SeguesTests.IntegrationTests.Orders
{
    public class OrderIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private const string TestUserId = "pedro-77";

        public OrderIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");

                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddDbContext<AppDbContext>(options =>
                        options.UseInMemoryDatabase("IntegDb_Order_Pedro")
                               .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

                    services.AddSingleton<IAntiforgery, NoOpAntiforgery>();

                    services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = "Test";
                        options.DefaultChallengeScheme = "Test";
                        options.DefaultScheme = "Test";
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", null);
                });
            });

            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", "User");

            SeedDatabase().GetAwaiter().GetResult();
        }

        private async Task SeedDatabase()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            var config = new AppConfig
            {
                BarOpeningTime = new TimeSpan(0, 0, 0),
                BarClosingTime = new TimeSpan(23, 59, 59),
                BarLink = "http://bar.test",
                CanteenLink = "http://canteen.test",
                IsOpenSaturday = true,
                IsOpenSunday = true,
                TicketValidityDays = 365
            };
            context.AppConfig.Add(config);

            var cat = new UserCategory { Name = "Estudante" };
            var pedro = new AppUser
            {
                Id = TestUserId,
                UserName = "Pedro",
                NormalizedUserName = "PEDRO",
                Email = "pedro@test.com",
                NormalizedEmail = "PEDRO@TEST.COM",
                FirstName = "Pedro",
                LastName = "Integracao",
                BirthDate = new DateTime(2000, 1, 1),
                Gender = Gender.Male,
                UserCategory = cat,
                Balance = 42.50m,
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            context.UserCategory.Add(cat);
            context.Users.Add(pedro);
            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task Index_Get_ReturnsSuccess_AndShowsCorrectBalance()
        {
            var response = await _client.GetAsync("/Order/Order/Index");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var html = await response.Content.ReadAsStringAsync();

            Assert.Contains("Saldo Disponível", html);
            Assert.Contains("42", html);
            Assert.Contains("50", html);

            Assert.Contains("Efetuar Pedido", html);
            Assert.Contains("Ver Pedidos", html);
        }
    }
}