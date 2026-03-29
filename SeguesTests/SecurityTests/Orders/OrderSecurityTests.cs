using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using SeguesTests.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace SeguesTests.SecurityTests.Orders
{
    public class OrderSecurityTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public OrderSecurityTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");

                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddDbContext<AppDbContext>(options =>
                        options.UseInMemoryDatabase("SecurityDb_Order_Pedro"));

                    var mockAdmin = new Mock<IAdminService>();
                    mockAdmin.Setup(s => s.GetScheduleAsync()).ReturnsAsync(new BarCanteenConfigViewModel
                    {
                        BarOpeningTime = new TimeSpan(8, 0, 0),
                        BarClosingTime = new TimeSpan(20, 0, 0)
                    });
                    services.AddSingleton(mockAdmin.Object);

                    var store = new Mock<IUserStore<AppUser>>();
                    var mockUserManager = new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);

                    var cat = new UserCategory { Name = "Estudante" };
                    var testUser = new AppUser
                    {
                        Id = "security-user",
                        UserName = "Pedro",
                        Email = "pedro@test.com",
                        FirstName = "Pedro",
                        LastName = "Segurança",
                        BirthDate = new DateTime(2000, 1, 1),
                        Gender = Gender.Male,
                        UserCategory = cat
                    };

                    mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(testUser);
                    mockUserManager.Setup(u => u.GetUsersInRoleAsync(It.IsAny<string>())).ReturnsAsync(new List<AppUser>());
                    mockUserManager.Setup(u => u.CreateAsync(It.IsAny<AppUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
                    mockUserManager.Setup(u => u.AddToRoleAsync(It.IsAny<AppUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
                    mockUserManager.Setup(u => u.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(testUser);

                    var userManagerDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(UserManager<AppUser>));
                    if (userManagerDescriptor != null) services.Remove(userManagerDescriptor);
                    services.AddScoped(sp => mockUserManager.Object);

                    var antiforgeryDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAntiforgery));
                    if (antiforgeryDescriptor != null) services.Remove(antiforgeryDescriptor);
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
        public async Task Index_Get_Unauthenticated_ReturnsUnauthorized()
        {
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

            var response = await client.GetAsync("/Order/Order/Index");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Index_Get_AuthenticatedUser_ReturnsSuccess()
        {
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", "User");

            var response = await client.GetAsync("/Order/Order/Index");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}