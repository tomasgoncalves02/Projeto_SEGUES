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
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using SeguesTests.Helpers;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Projeto_SEGUES;
using Xunit;

namespace SeguesTests.RegressionTests.Orders
{
    public class OrderRegressionTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public OrderRegressionTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");

                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddDbContext<AppDbContext>(options =>
                        options.UseInMemoryDatabase("RegressionDb_Order_Pedro")
                               .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

                    var mockAdmin = new Mock<IAdminService>();
                    mockAdmin.Setup(s => s.GetScheduleAsync()).ReturnsAsync(new BarCanteenConfigViewModel
                    {
                        BarOpeningTime = new TimeSpan(8, 0, 0),
                        BarClosingTime = new TimeSpan(20, 0, 0)
                    });
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

            context.Database.EnsureCreated();

            if (!context.Users.Any(u => u.Id == "pedro-77"))
            {
                var cat = context.UserCategory.FirstOrDefault(c => c.Name == "Estudante");
                if (cat == null)
                {
                    cat = new UserCategory { Name = "Estudante" };
                    context.UserCategory.Add(cat);
                }

                var pedro = new AppUser
                {
                    Id = "pedro-77",
                    UserName = "Pedro",
                    Email = "pedro@test.com",
                    FirstName = "Pedro",
                    LastName = "Regressao",
                    BirthDate = new DateTime(2000, 1, 1),
                    Gender = Gender.Male,
                    UserCategory = cat,
                    Balance = 10.00m
                };

                context.Users.Add(pedro);
                await context.SaveChangesAsync();
            }
        }

        [Fact]
        public async Task Index_Get_ReturnsOk_DoesNotRegress()
        {
            var response = await _client.GetAsync("/Order/Order/Index");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}