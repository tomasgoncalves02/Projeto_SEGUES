using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Inventory;
using Projeto_SEGUES.Models.Order;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using SeguesTests.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Projeto_SEGUES;
using Xunit;

namespace SeguesTests.IntegrationTests.Orders
{
    public class CreateOrderIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public CreateOrderIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");

                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddDbContext<AppDbContext>(options =>
                        options.UseInMemoryDatabase("IntegDb_CreateOrder_Pedro")
                               .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

                    var mockAdmin = new Mock<IAdminService>();
                    mockAdmin.Setup(s => s.GetMenuLinksAsync()).ReturnsAsync(new BarCanteenConfigViewModel());
                    mockAdmin.Setup(s => s.GetScheduleAsync()).ReturnsAsync(new BarCanteenConfigViewModel());
                    mockAdmin.Setup(s => s.IsBarOpenAsync(It.IsAny<TimeSpan>())).ReturnsAsync(true);

                    services.AddSingleton(mockAdmin.Object);

                    var antiforgeryDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAntiforgery));
                    if (antiforgeryDescriptor != null) services.Remove(antiforgeryDescriptor);
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

            var cat = new UserCategory { Name = "Estudante" };
            var pedro = new AppUser
            {
                Id = "pedro-77",
                UserName = "Pedro",
                Email = "pedro@test.com",
                FirstName = "Pedro",
                LastName = "Integração",
                BirthDate = new DateTime(2000, 1, 1),
                Gender = Gender.Male,
                UserCategory = cat,
                Balance = 50.00m
            };

            var prodCategory = new ProductCategory { Name = "Snacks", Description = "Comida" };
            var product = new Product
            {
                Id = 1,
                Name = "Tosta",
                Description = "Tosta Mista",
                Price = 2.00m,
                Category = prodCategory,
                Stock = 20,
                MinimumStock = 5
            };

            context.UserCategory.Add(cat);
            context.Users.Add(pedro);
            context.ProductCategory.Add(prodCategory);
            context.Product.Add(product);

            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task Index_Get_ReturnsSuccessAndDisplaysProducts()
        {
            var response = await _client.GetAsync("/Order/CreateOrder/Index");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("Tosta", html);
        }

        [Fact]
        public async Task AddToCart_Post_ValidProduct_AddsToDatabase()
        {
            var formContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "id", "1" },
                { "qty", "2" }
            });

            var response = await _client.PostAsync("/Order/CreateOrder/AddToCart", formContent);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var cart = await context.Order
                .Include(o => o.ProductPurchases)
                .FirstOrDefaultAsync(o => o.AppUser.Id == "pedro-77");

            Assert.NotNull(cart);
            Assert.Single(cart.ProductPurchases);
            Assert.Equal(2, cart.ProductPurchases.First().Quantity);
        }
    }
}