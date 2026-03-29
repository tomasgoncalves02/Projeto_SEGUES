using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Inventory;
using Projeto_SEGUES.Models.Order;
using Projeto_SEGUES.Models.User;
using SeguesTests.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Xunit;

namespace SeguesTests.IntegrationTests.Orders
{
    public class ActiveOrderIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public ActiveOrderIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddDbContext<AppDbContext>(options =>
                        options.UseInMemoryDatabase("IntegDb_ActiveOrders_Pedro")
                               .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

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
                LastName = "Estudante",
                BirthDate = DateTime.Now.AddYears(-20),
                Gender = Gender.Male,
                UserCategory = cat
            };

            var prodCategory = new ProductCategory { Name = "Bebidas", Description= "Liquidos" };
            var product = new Product
            {
                Name = "agua",
                Description = "Liquida",
                Price = 1.00m,
                Category = prodCategory,
                Stock = 100,
                MinimumStock = 10
            };

            var order = new Order
            {
                Id = 1,
                AppUser = pedro,
                Status = OrderStatus.Pending,
                OrderDate = DateTime.Now
            };

            var orderLine = new OrderLine
            {
                Order = order,
                ProductId = product.Id,
                OrderId = order.Id,
                Product = product,
                Quantity = 2,
                ProductValue = 1.00m
            };

            order.ProductPurchases = new List<OrderLine> { orderLine };

            context.UserCategory.Add(cat);
            context.Users.Add(pedro);
            context.ProductCategory.Add(prodCategory);
            context.Product.Add(product);
            context.Order.Add(order);

            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task Index_Get_ReturnsSuccess()
        {
            var response = await _client.GetAsync("/Order/ActiveOrder/Index");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task OrderDetails_Get_ValidOrder_ReturnsSuccess()
        {
            var response = await _client.GetAsync("/Order/ActiveOrder/OrderDetails/1");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("agua", html);
        }

        [Fact]
        public async Task CancelOrder_Post_ValidOrder_RedirectsToIndexAndUpdatesDb()
        {
            var formContent = new FormUrlEncodedContent(new Dictionary<string, string>());

            var response = await _client.PostAsync("/Order/ActiveOrder/CancelOrder/1", formContent);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("ActiveOrder", response.Headers.Location?.OriginalString ?? "");

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var canceledOrder = await context.Order.AsNoTracking().FirstOrDefaultAsync(o => o.Id == 1);

            Assert.NotNull(canceledOrder);
            Assert.Equal(OrderStatus.Cancelled, canceledOrder.Status);
        }
    }
}