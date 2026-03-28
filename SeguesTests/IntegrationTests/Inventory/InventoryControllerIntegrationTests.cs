using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Services;
using SeguesTests.Helpers;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace SeguesTests.IntegrationTests.Inventory
{
    public class InventoryControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public InventoryControllerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("InventoryIntDb"));

                    var adminDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAdminService));
                    if (adminDescriptor != null) services.Remove(adminDescriptor);

                    var mockAdmin = new Mock<IAdminService>();
                    mockAdmin.Setup(s => s.GetMenuLinksAsync())
                             .ReturnsAsync(new BarCanteenConfigViewModel { CanteenMenuLink = "/canteen", BarMenuLink = "/bar" });
                    services.AddSingleton<IAdminService>(mockAdmin.Object);

                    services.AddAuthentication("Test")
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", null);
                });
            });
        }

        [Fact]
        public async Task Index_AuthenticatedUser_ReturnsSuccessStatusCode()
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/Inventory/Inventory/Index");

            response.EnsureSuccessStatusCode();
            Assert.Equal("text/html; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        }

        [Fact]
        public async Task Index_UnauthenticatedUser_ReturnsRedirectToLogin()
        {
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            client.DefaultRequestHeaders.Authorization = null;

            var response = await client.GetAsync("/Inventory/Inventory/Index");

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

            var location = response.Headers.Location?.ToString() ?? "";
            Assert.Contains("ReturnUrl", location, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}