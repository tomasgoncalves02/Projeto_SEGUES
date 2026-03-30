using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Services;
using SeguesTests.Helpers;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Projeto_SEGUES;
using Xunit;

namespace SeguesTests.IntegrationTests.Home
{
    public class HomeControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public HomeControllerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null) services.Remove(descriptor);
                    services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("HomeIntegrationDb"));

                    var adminDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAdminService));
                    if (adminDescriptor != null) services.Remove(adminDescriptor);

                    var mockAdmin = new Mock<IAdminService>();
                    mockAdmin.Setup(s => s.GetMenuLinksAsync())
                             .ReturnsAsync(new BarCanteenConfigViewModel { CanteenMenuLink = "/canteen", BarMenuLink = "/bar" });
                    services.AddSingleton<IAdminService>(mockAdmin.Object);

                    var orderDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IOrderService));
                    if (orderDescriptor != null) services.Remove(orderDescriptor);

                    var mockOrder = new Mock<IOrderService>();
                    services.AddSingleton<IOrderService>(mockOrder.Object);

                    services.AddAuthentication("Test")
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", null);
                });
            });
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task Index_AuthenticatedUser_ReturnsSuccessStatusCode()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();

                var cat = new UserCategory { Name = "Cliente" };
                db.UserCategory.Add(cat);

                var user = new Student
                {
                    Id = "pedro-77",
                    UserName = "Pedro",
                    Email = "pedro@segues.pt",
                    FirstName = "Pedro",
                    LastName = "Original",
                    BirthDate = new DateTime(2000, 1, 1),
                    Gender = Gender.Male,
                    UserCategory = cat,
                    StudentNumber = "2024001"
                };

                db.Users.Add(user);
                await db.SaveChangesAsync();
            }

            var response = await _client.GetAsync("/Home/Index");

            if (!response.IsSuccessStatusCode)
            {
                var errorHtml = await response.Content.ReadAsStringAsync();
                throw new Exception($"Erro Servidor: {errorHtml}");
            }

            response.EnsureSuccessStatusCode();
            Assert.Equal("text/html; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        }

        [Fact]
        public async Task Privacy_ReturnsSuccessStatusCode()
        {
            var response = await _client.GetAsync("/Home/Privacy");
            response.EnsureSuccessStatusCode();
            Assert.Equal("text/html; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        }
    }
}